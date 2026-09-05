import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { AgentChatResponse } from '../models/agent.model';
import { API_BASE_URL } from '../config/api.config';
import { TenantService } from './tenant.service';

export type SignalRConnectionStatus = 'connected' | 'connecting' | 'reconnecting' | 'disconnected';

@Injectable({
  providedIn: 'root',
})
export class AgentApiService {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly tenantService = inject(TenantService);

  private readonly apiUrl = `${API_BASE_URL}/api/agent/chat`;
  private readonly hubUrl = `${API_BASE_URL}/hubs/agent`;

  private hubConnection: HubConnection | null = null;

  private readonly thoughtProcessSubject = new Subject<string>();
  readonly thoughtProcess$: Observable<string> = this.thoughtProcessSubject.asObservable();

  private readonly finalResponseSubject = new Subject<string>();
  readonly finalResponse$: Observable<string> = this.finalResponseSubject.asObservable();

  private readonly connectionStatusSubject = new Subject<SignalRConnectionStatus>();
  readonly connectionStatus$: Observable<SignalRConnectionStatus> = this.connectionStatusSubject.asObservable();

  /**
   * Initializes or returns the active SignalR HubConnection to /hubs/agent in browser environments.
   */
  async startConnection(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    if (this.hubConnection && this.hubConnection.state === 'Connected') {
      return;
    }

    const tenantId = this.tenantService.currentTenantId();
    const targetUrl = tenantId ? `${this.hubUrl}?tenantId=${tenantId}` : this.hubUrl;

    if (!this.hubConnection) {
      this.hubConnection = new HubConnectionBuilder()
        .withUrl(targetUrl)
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .configureLogging(LogLevel.Information)
        .build();

      this.hubConnection.on('ReceiveThoughtProcess', (message: string) => {
        this.thoughtProcessSubject.next(message);
      });

      this.hubConnection.on('ReceiveFinalResponse', (response: string) => {
        this.finalResponseSubject.next(response);
      });

      this.hubConnection.onreconnecting((error) => {
        console.warn('SignalR reconnecting to AgentHub...', error);
        this.connectionStatusSubject.next('reconnecting');
      });

      this.hubConnection.onreconnected(() => {
        console.info('SignalR reconnected to AgentHub.');
        this.connectionStatusSubject.next('connected');
      });

      this.hubConnection.onclose((error) => {
        console.warn('SignalR connection to AgentHub closed.', error);
        this.connectionStatusSubject.next('disconnected');
      });
    }

    try {
      this.connectionStatusSubject.next('connecting');
      await this.hubConnection.start();
      this.connectionStatusSubject.next('connected');
    } catch (err) {
      this.connectionStatusSubject.next('disconnected');
      console.warn('SignalR AgentHub initial connection error:', err);
    }
  }

  /**
   * Dispatches a prompt to the Semantic Kernel agent orchestrator via the real-time SignalR hub.
   * Returns the final response directly from the hub invocation.
   *
   * @param prompt The natural language inquiry
   */
  async sendPrompt(prompt: string): Promise<string> {
    await this.startConnection();
    if (this.hubConnection && this.hubConnection.state === 'Connected') {
      const response = await this.hubConnection.invoke<string>('SendPrompt', prompt);
      return response;
    } else {
      throw new Error('Real-time connection to AgentHub is currently offline.');
    }
  }

  /**
   * Dispatches a prompt over SignalR and returns an Observable emitting the final response.
   * Falls back to HTTP POST /api/agent/chat if the WebSocket hub is unavailable.
   *
   * @param prompt The user's query or instruction
   * @returns Observable emitting the agent response payload
   */
  sendMessage(prompt: string): Observable<AgentChatResponse> {
    return new Observable<AgentChatResponse>((observer) => {
      this.sendPrompt(prompt)
        .then((response) => {
          observer.next({ response });
          observer.complete();
        })
        .catch((hubErr) => {
          console.warn('Hub dispatch failed, falling back to HTTP POST /api/agent/chat...', hubErr);
          this.http.post<AgentChatResponse>(this.apiUrl, { prompt }).subscribe({
            next: (res) => {
              observer.next(res);
              observer.complete();
            },
            error: (httpErr) => observer.error(httpErr),
          });
        });
    });
  }
}
