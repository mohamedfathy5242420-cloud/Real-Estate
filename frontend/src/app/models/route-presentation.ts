export interface RoutePresentationData {
  eyebrow: string;
  title: string;
  description: string;
  indexable: boolean;
}

export function isRoutePresentationData(value: unknown): value is RoutePresentationData {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate['eyebrow'] === 'string' &&
    typeof candidate['title'] === 'string' &&
    typeof candidate['description'] === 'string' &&
    typeof candidate['indexable'] === 'boolean'
  );
}
