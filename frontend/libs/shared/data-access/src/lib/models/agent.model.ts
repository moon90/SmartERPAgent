export interface ThoughtProcessUpdate {
  message: string;
  timestamp: Date;
}

export interface ChatMessage {
  id: string;
  role: 'user' | 'agent';
  content: string;
  thoughtProcess?: string;
  timestamp: Date;
}

export interface AgentChatRequest {
  prompt: string;
}

export interface AgentChatResponse {
  response: string;
}

export interface AgentPromptRequest {
  readonly prompt: string;
  readonly tenantId?: string;
  readonly sessionId?: string;
  readonly contextParameters?: Record<string, unknown>;
}

export interface AgentActionExecuted {
  readonly pluginName: string;
  readonly functionName: string;
  readonly argumentsJson: string;
  readonly resultJson: string;
}

export interface AgentResponse {
  readonly content: string;
  readonly thoughtProcess?: string;
  readonly executedActions?: readonly AgentActionExecuted[];
}
