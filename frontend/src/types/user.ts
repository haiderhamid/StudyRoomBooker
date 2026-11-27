// What .NET calls the role claim in the token
export const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

export interface User {
  sub: string;
  exp: number;
  [ROLE_CLAIM]?: string;
}
