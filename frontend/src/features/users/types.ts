import type { AuditMetadata } from '@/shared/api/contracts';

export interface UserListItem extends AuditMetadata {
  id: string;
  displayName: string | null;
  email: string | null;
  emailConfirmed: boolean;
  status: 'ACTIVE' | 'SUSPENDED' | 'DELETED';
  lastLoginAt: string | null;
  roles: { id: string; name: string; isActive: boolean }[];
}
