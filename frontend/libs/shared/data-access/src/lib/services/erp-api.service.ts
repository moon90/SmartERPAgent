import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Invoice, CreateInvoiceRequest } from '../models/invoice.model';
import { InventoryItem, CreateInventoryItemRequest, UpdateStockRequest } from '../models/inventory.model';
import { AgentPromptRequest, AgentResponse } from '../models/agent.model';
import { API_BASE_URL } from '../config/api.config';

@Injectable({
  providedIn: 'root',
})
export class ErpApiService {
  private readonly baseUrl = `${API_BASE_URL}/api`;

  constructor(private readonly http: HttpClient) {}

  // Invoices
  getInvoices(): Observable<Invoice[]> {
    return this.http.get<Invoice[]>(`${this.baseUrl}/invoices`);
  }

  getInvoiceById(id: string): Observable<Invoice> {
    return this.http.get<Invoice>(`${this.baseUrl}/invoices/${id}`);
  }

  createInvoice(request: CreateInvoiceRequest): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/invoices`, request);
  }

  // Inventory
  getInventory(): Observable<InventoryItem[]> {
    return this.http.get<InventoryItem[]>(`${this.baseUrl}/inventory`);
  }

  getLowStockItems(): Observable<InventoryItem[]> {
    return this.http.get<InventoryItem[]>(`${this.baseUrl}/inventory/low-stock`);
  }

  createInventoryItem(request: CreateInventoryItemRequest): Observable<InventoryItem> {
    return this.http.post<InventoryItem>(`${this.baseUrl}/inventory`, request);
  }

  updateStock(id: string, request: UpdateStockRequest): Observable<InventoryItem> {
    return this.http.patch<InventoryItem>(`${this.baseUrl}/inventory/${id}/stock`, request);
  }

  // Agent AI Copilot
  executeAgentPrompt(request: AgentPromptRequest): Observable<AgentResponse> {
    return this.http.post<AgentResponse>(`${this.baseUrl}/agent/prompt`, request);
  }
}
