import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../models/problem-details.model';

const SERVER_UNAVAILABLE_MESSAGE = 'לא ניתן להתחבר לשרת. ודאו שהשרת פועל ונסו שוב.';

/**
 * Turns an HTTP error into a message for the user.
 * Uses the Problem Details "detail" sent by the API when available.
 */
export function toErrorMessage(error: HttpErrorResponse): string {
  // No response at all (e.g. the browser could not reach the server).
  if (error.status === 0) {
    return SERVER_UNAVAILABLE_MESSAGE;
  }

  const problem = isProblemDetails(error.error) ? error.error : null;

  // Every error from our API carries Problem Details. A 5xx without it came from the
  // development proxy or the network, which means the API itself is not reachable.
  if (!problem && error.status >= 500) {
    return SERVER_UNAVAILABLE_MESSAGE;
  }

  if (problem?.errors) {
    return Object.values(problem.errors).flat().join(' ');
  }

  return problem?.detail ?? problem?.title ?? 'אירעה שגיאה בלתי צפויה. נסו שוב.';
}

function isProblemDetails(body: unknown): body is ProblemDetails {
  return typeof body === 'object' && body !== null && ('title' in body || 'status' in body);
}