export interface ApiResponse<T> {
  data: T;
  traceId: string;
  correlationId: string;
  message?: string;
}

export interface AuditMetadata {
  createdAt: string;
  updatedAt: string;
  createdBy: string | null;
  updatedBy: string | null;
}

export interface ConcurrentMetadata extends AuditMetadata {
  concurrencyToken: string;
}

export interface PaginatedResponse<T> extends ApiResponse<T[]> {
  pagination: {
    pageNumber: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
  };
}

export interface ProblemDetails {
  status?: number;
  code?: string;
  traceId?: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
}
