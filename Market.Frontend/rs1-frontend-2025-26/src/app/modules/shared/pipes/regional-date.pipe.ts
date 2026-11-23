import { Pipe, PipeTransform } from '@angular/core';

type RegionalDateFormat = 'date' | 'time' | 'date-time';

@Pipe({ name: 'regionalDate', standalone: false })
export class RegionalDatePipe implements PipeTransform {
  private readonly formatter = new Intl.DateTimeFormat('bs-BA', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23',
    timeZone: 'Europe/Sarajevo'
  });

  transform(value: string | Date | null | undefined, format: RegionalDateFormat = 'date-time'): string {
    if (!value) return '';
    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) return '';

    const parts = Object.fromEntries(this.formatter.formatToParts(date).map(part => [part.type, part.value]));
    const dateLabel = `${parts['day']}.${parts['month']}.${parts['year']}.`;
    const timeLabel = `${parts['hour']}:${parts['minute']}`;

    if (format === 'date') return dateLabel;
    if (format === 'time') return timeLabel;
    return `${dateLabel} ${timeLabel}`;
  }
}
