import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../models/problem-details.model';

/**
 * Turns an HTTP error into a message for the user.
 * Uses the Problem Details "detail" sent by the API when available.
 */
export function toErrorMessage(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'לא ניתן להתחבר לשרת. ודאו שהשרת פועל ונסו שוב.';
  }

  const problem = error.error as ProblemDetails | null;

  if (problem?.errors) {
    return Object.values(problem.errors).flat().join(' ');
  }

  return problem?.detail ?? problem?.title ?? 'אירעה שגיאה בלתי צפויה. נסו שוב.';
}