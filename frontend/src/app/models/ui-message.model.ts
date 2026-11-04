// A short message shown to the user after an action (e.g. a status update).
export interface UiMessage {
  kind: 'success' | 'error';
  text: string;
}