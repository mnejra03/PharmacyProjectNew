import { HttpParams } from '@angular/common/http';

/**
 * Convert an object to Angular HttpParams, including nested objects.
 *
 * Examples:
 *  { paging: { page: 1, pageSize: 20 } } → ?paging.page=1&paging.pageSize=20
 *  { ids: [1, 2, 3] }                     → ?ids=1&ids=2&ids=3
 *  { search: null }                       → (skip this value)
 */
export function buildHttpParams(obj: Record<string, any>, prefix: string = ''): HttpParams {
  let params = new HttpParams();

  if (obj === undefined || obj === null) {
    return params;
  }

  Object.entries(obj).forEach(([key, value]) => {
    // Build the full key (npr. "paging.page")
    const fullKey = prefix ? `${prefix}.${key}` : key;

    // 1) null or undefined: skip
    if (value === null || value === undefined) {
      return;
    }

    // 2) empty strings: skip
    if (typeof value === 'string' && value.trim() === '') {
      return;
    }

    // 3) arrays: append each element separately
    if (Array.isArray(value)) {
      value.forEach(val => {
        if (val !== null && val !== undefined) {
          params = params.append(fullKey, String(val));
        }
      });
      return;
    }

    // 4) objects: flatten recursively
    if (typeof value === 'object' && !(value instanceof Date)) {
      const nestedParams = buildHttpParams(value, fullKey);
      nestedParams.keys().forEach(nestedKey => {
        nestedParams.getAll(nestedKey)?.forEach(nestedValue => {
          params = params.append(nestedKey, nestedValue);
        });
      });
      return;
    }

    // 5) Date: convert to an ISO string
    if (value instanceof Date) {
      params = params.set(fullKey, value.toISOString());
      return;
    }

    // 6) all other values are serialized as strings
    params = params.set(fullKey, String(value));
  });

  return params;
}
