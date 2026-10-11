import type { AuditMetadata } from '@/shared/api/contracts';

export interface Permission extends AuditMetadata {
  id: string;
  code: string;
  name: string;
  description: string | null;
}
export interface Role extends AuditMetadata {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
  isDefaultForRegistration: boolean;
  concurrencyStamp: string;
  permissions: Permission[];
}
export interface RoleInput {
  name: string;
  description?: string | null;
  isActive: boolean;
  isDefaultForRegistration: boolean;
}
export interface RoleMatrix {
  permissions: Permission[];
  roles: {
    items: Role[];
    pageNumber: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
  };
}
