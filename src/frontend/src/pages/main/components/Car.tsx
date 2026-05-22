export interface CarState {
  id: string;
  x: number;
  y: number;
  angle: number;
  visible: boolean;
  color: string;
  accent: string;
}

export default function Car({ car }: { car: CarState }) {
  return (
    <g
      transform={`translate(${car.x} ${car.y}) rotate(${car.angle}) scale(0.8)`}
      opacity={car.visible ? 1 : 0}
    >
      <rect x="-35" y="-19" width="70" height="38" rx="12" fill={car.accent} />
      <rect x="-28" y="-15" width="56" height="30" rx="10" fill={car.color} />
      <rect x="-10" y="-11" width="20" height="22" rx="5" fill="#142033" />
    </g>
  );
}
