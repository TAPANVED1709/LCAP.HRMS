import type { Master, Row } from './master-config';

/** Only writable DTO fields are sent, including fields absent from the table. */
export function requestBody(master: Master, values: Row): Row {
  return Object.fromEntries(
    master.fields.map((field) => {
      let value = values[field.key] ?? null;
      if (typeof value === 'string') {
        value = value.trim();
        if (value === '') value = null;
      }
      if (field.type === 'time' && value?.length === 5) value += ':00';
      if (field.type === 'checkbox') value = !!value;
      return [field.key, value];
    }),
  );
}
export function crossFieldError(resource: string, body: Row): string {
  if (resource === 'shifts') {
    if (body['startTime'] === body['endTime']) return 'Start and end times must differ.';
    if (
      body['minimumHalfDayMinutes'] !== null &&
      body['minimumFullDayMinutes'] !== null &&
      body['minimumHalfDayMinutes'] > body['minimumFullDayMinutes']
    )
      return 'Half-day minutes cannot exceed full-day minutes.';
  }
  if (resource === 'work-locations') {
    if ((body['latitude'] === null) !== (body['longitude'] === null))
      return 'Provide both coordinates or leave both blank.';
    if (
      ['latitude', 'longitude'].some(
        (k) => body[k] !== null && Math.abs(body[k] * 1e7 - Math.round(body[k] * 1e7)) > 0.000001,
      )
    )
      return 'Coordinates support up to seven decimal places.';
  }
  return '';
}
