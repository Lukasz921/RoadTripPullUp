export interface RoadConfig {
  id: string;
  d: string;
  car: {
    start: number;
    end: number;
    color: string;
    accent: string;
  };
}

export const ROADS: RoadConfig[] = [
  {
    id: 'silver',
    d: 'M162 115 L162 563 L797 563 L797 844',
    car: { start: 0.02, end: 0.62, color: '#94a3b8', accent: '#334155' },
  },
  {
    id: 'red',
    d: 'M142 734 L577 734 L577 169 L908 169',
    car: { start: 0.02, end: 0.62, color: '#ef4444', accent: '#7f1d1d' },
  },
  {
    id: 'green',
    d: 'M106 380 L969 380',
    car: { start: 0.16, end: 0.72, color: '#22c55e', accent: '#14532d' },
  },
];
