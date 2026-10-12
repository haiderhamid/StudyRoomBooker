export const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

export interface User {
  sub: string;
  email: string;
  nameid: string;
  jti: string;
  exp: number;
  iss: string;
  aud: string;
  [ROLE_CLAIM]?: string;
}
