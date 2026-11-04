// Sent by the API as strings (JsonStringEnumConverter).
export type RequestStatus = 'New' | 'InProgress' | 'Waiting' | 'Completed';

export const REQUEST_STATUSES: RequestStatus[] = ['New', 'InProgress', 'Waiting', 'Completed'];

// Display names for the UI. The API always uses the English values.
export const STATUS_LABELS: Record<RequestStatus, string> = {
  New: 'חדשה',
  InProgress: 'בטיפול',
  Waiting: 'ממתינה',
  Completed: 'הסתיימה'
};

// Same rules as the server (StatusTransitions). Used only to offer sensible choices;
// the server still enforces them.
export const ALLOWED_TRANSITIONS: Record<RequestStatus, RequestStatus[]> = {
  New: ['InProgress'],
  InProgress: ['Waiting', 'Completed'],
  Waiting: ['InProgress'],
  Completed: []
};