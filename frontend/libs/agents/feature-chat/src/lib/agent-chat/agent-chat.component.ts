import {
  Component,
  ElementRef,
  ViewChild,
  inject,
  OnInit,
  OnDestroy,
  AfterViewChecked
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import {
  AgentApiService,
  ChatMessage,
  AgentChatResponse,
  SignalRConnectionStatus
} from '@smart-erp/data-access';

@Component({
  selector: 'smart-erp-agent-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './agent-chat.component.html',
  styleUrl: './agent-chat.component.scss',
})
export class AgentChatComponent implements OnInit, OnDestroy, AfterViewChecked {
  private readonly agentApi = inject(AgentApiService);
  private readonly subscriptions = new Subscription();

  @ViewChild('scrollContainer') private readonly scrollContainer?: ElementRef<HTMLDivElement>;

  private shouldScrollToBottom = false;

  isCollapsed = false;
  isLoading = false;
  userPrompt = '';
  streamingThought: string | null = null;
  connectionStatus: SignalRConnectionStatus = 'connecting';

  messages: ChatMessage[] = [
    {
      id: 'welcome',
      role: 'agent',
      content: 'Hello! I am your Smart ERP Copilot powered by ASP.NET Core SignalR real-time streaming. Ask me to check inventory stock levels, summarize overdue invoices, or inspect tenant ledger records.',
      timestamp: new Date(),
    },
  ];

  ngOnInit(): void {
    // 1. Initialize real-time connection to /hubs/agent
    this.agentApi.startConnection();

    // 2. Subscribe to real-time thought updates
    this.subscriptions.add(
      this.agentApi.thoughtProcess$.subscribe((thought) => {
        this.streamingThought = thought;
        this.shouldScrollToBottom = true;
      })
    );

    // 3. Subscribe to connection status changes
    this.subscriptions.add(
      this.agentApi.connectionStatus$.subscribe((status) => {
        this.connectionStatus = status;
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  toggleCollapse(): void {
    this.isCollapsed = !this.isCollapsed;
  }

  sendPreset(prompt: string): void {
    this.userPrompt = prompt;
    this.onSendMessage();
  }

  onSendMessage(): void {
    const text = this.userPrompt.trim();
    if (!text || this.isLoading) {
      return;
    }

    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content: text,
      timestamp: new Date(),
    };

    this.messages.push(userMessage);
    this.userPrompt = '';
    this.isLoading = true;
    this.streamingThought = 'Connecting to Semantic Kernel agent...';
    this.shouldScrollToBottom = true;

    // Dispatch via real-time SignalR hub (with automatic HTTP fallback)
    this.agentApi.sendMessage(text).subscribe({
      next: (res: AgentChatResponse) => {
        const agentMessage: ChatMessage = {
          id: crypto.randomUUID(),
          role: 'agent',
          content: res.response,
          thoughtProcess: this.streamingThought || undefined,
          timestamp: new Date(),
        };
        this.messages.push(agentMessage);
        this.streamingThought = null;
        this.isLoading = false;
        this.shouldScrollToBottom = true;
      },
      error: (err) => {
        const errorMessage: ChatMessage = {
          id: crypto.randomUUID(),
          role: 'agent',
          content: `Notice: ${err.error?.message || err.message || 'Unable to connect to the agent service.'}`,
          timestamp: new Date(),
        };
        this.messages.push(errorMessage);
        this.streamingThought = null;
        this.isLoading = false;
        this.shouldScrollToBottom = true;
      },
    });
  }

  private scrollToBottom(): void {
    if (this.scrollContainer?.nativeElement) {
      const el = this.scrollContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
    }
  }
}
