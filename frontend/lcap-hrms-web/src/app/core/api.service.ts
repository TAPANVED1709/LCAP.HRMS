import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { Row } from '../organisation/master-config';
export interface Envelope<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
  traceId: string;
}
@Injectable({ providedIn: 'root' })
export class SessionService {
  token = signal('');
  companyName = signal('LCAP');
}
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(SessionService).token();
  return next(
    token && request.url.startsWith('/api/')
      ? request.clone({ setHeaders: { Authorization: 'Bearer ' + token } })
      : request,
  );
};
@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  async all(resource: string): Promise<Row[]> {
    const rows: Row[] = [];
    for (let skip = 0; ; skip += 1000) {
      const response = await firstValueFrom(
        this.http.get<Envelope<Row[]>>('/api/' + resource, { params: { skip, take: 1000 } }),
      );
      if (!response.success) throw new Error(response.message);
      rows.push(...response.data);
      if (response.data.length < 1000) return rows;
    }
  }
  get(resource: string, id: string) {
    return firstValueFrom(this.http.get<Envelope<Row>>('/api/' + resource + '/' + id));
  }
  save(resource: string, body: Row, id?: string) {
    return firstValueFrom(
      id
        ? this.http.put<Envelope<Row>>('/api/' + resource + '/' + id, body)
        : this.http.post<Envelope<Row>>('/api/' + resource, body),
    );
  }
  remove(resource: string, id: string) {
    return firstValueFrom(this.http.delete<void>('/api/' + resource + '/' + id));
  }
}
export function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 401)
      return 'Your session is not connected or has expired. Open Profile → API connection to provide a valid access token.';
    if (error.status === 403) return 'Your account does not have permission for this action.';
    if (error.status === 0 || error.status === 502 || error.status === 504)
      return 'We could not reach the HRMS service. Check your connection and try again.';
    const details = error.error?.errors;
    if (Array.isArray(details) && details.length) return details.join(' ');
    return error.error?.message || 'The request could not be completed. Please try again.';
  }
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.';
}
