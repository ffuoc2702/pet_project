import type { AuditMetadata } from '@/shared/api/contracts';
export interface SportFormat extends AuditMetadata {
  id: string;
  code: string;
  displayName: string;
  sideSize: number;
  description: string | null;
  isActive: boolean;
}
export interface Sport extends AuditMetadata {
  id: string;
  code: string;
  name: string;
  description: string | null;
  isActive: boolean;
  formats: SportFormat[];
}
export interface SportInput {
  code: string;
  name: string;
  description?: string;
  formats: {
    id?: string;
    code?: string;
    displayName?: string;
    sideSize: number;
    description?: string;
    action: 'NEW' | 'MODIFY' | 'DELETE';
  }[];
}
