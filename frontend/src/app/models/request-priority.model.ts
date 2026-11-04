export type RequestPriority = 'Low' | 'Medium' | 'High';

export const REQUEST_PRIORITIES: RequestPriority[] = ['Low', 'Medium', 'High'];

// Display names for the UI. The API always uses the English values.
export const PRIORITY_LABELS: Record<RequestPriority, string> = {
  Low: 'נמוכה',
  Medium: 'בינונית',
  High: 'גבוהה'
};