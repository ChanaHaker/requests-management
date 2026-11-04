// RFC 7807 error body returned by the API (400, 404, 409, 500).
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}