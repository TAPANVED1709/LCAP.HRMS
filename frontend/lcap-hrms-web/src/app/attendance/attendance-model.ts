export interface AttendanceRow {
  attendanceId: string;
  employeeId: string;
  employeeCode: string;
  employeeName: string;
  branch: string;
  attendanceDate: string;
  timeZoneId: string;
  checkInTime: string | null;
  checkOutTime: string | null;
  workLocationName: string;
  distanceMeters: number | null;
  withinGeofence: boolean | null;
  status: number | string;
}
export interface Today {
  attendanceDate: string;
  timeZoneId: string;
  record: AttendanceRow | null;
}
export function attendanceState(row: AttendanceRow | null | undefined) {
  return !row ? 'Not Checked In' : row.checkOutTime ? 'Completed' : 'Checked In';
}
export function locate(
  geo: Geolocation | undefined,
): Promise<{
  latitude: number;
  longitude: number;
  accuracyMeters: number;
  clientTimestamp: string;
}> {
  return new Promise((resolve, reject) => {
    if (!geo) {
      reject(new Error('Location is unavailable. Use a supported browser over HTTPS.'));
      return;
    }
    geo.getCurrentPosition(
      (p) =>
        resolve({
          latitude: p.coords.latitude,
          longitude: p.coords.longitude,
          accuracyMeters: p.coords.accuracy,
          clientTimestamp: new Date(p.timestamp).toISOString(),
        }),
      (e) =>
        reject(
          new Error(
            e.code === 1
              ? 'Location permission is required to mark attendance.'
              : e.code === 3
                ? 'Location request timed out. Please try again.'
                : 'Your position is unavailable. Enable precise location and try again.',
          ),
        ),
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 },
    );
  });
}

export async function captureAndSubmit<T>(
  geo: Geolocation | undefined,
  submit: (position: Awaited<ReturnType<typeof locate>>) => Promise<T>,
  phase: (value: string) => void,
): Promise<T> {
  phase('Locating…');
  try {
    const position = await locate(geo);
    phase('Saving…');
    return await submit(position);
  } finally {
    phase('');
  }
}
