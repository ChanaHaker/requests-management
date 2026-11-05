// HTTP headers accept only ASCII, so the user name (X-User-Name) is limited to Latin characters.
export const USER_NAME_PATTERN = /^[A-Za-z0-9._-]{1,100}$/;

export const DEFAULT_USER_NAME = 'chana.haker';

export function isValidUserName(value: string): boolean {
  return USER_NAME_PATTERN.test(value.trim());
}