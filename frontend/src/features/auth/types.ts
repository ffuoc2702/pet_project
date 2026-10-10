export interface CurrentUser {
  id: string;
  email: string;
  displayName: string;
  emailVerified: boolean;
  roles: string[];
  permissions: string[];
}
export interface WebSession {
  accessToken: string;
  tokenType: string;
  accessTokenExpiresAt: string;
  user: CurrentUser;
}
