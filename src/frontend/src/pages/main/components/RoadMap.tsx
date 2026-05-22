import { useEffect, useRef, useState } from 'react';
import { ROADS } from '../../../data/roads';
import { clamp } from '../../../utils/math';
import Car, { type CarState } from './Car';

interface RoadMapProps {
  sectionRef: React.RefObject<HTMLElement | null>;
}

export default function RoadMap({ sectionRef }: RoadMapProps) {
  const pathRefs = useRef<(SVGPathElement | null)[]>([]);
  const [cars, setCars] = useState<CarState[]>([]);

  useEffect(() => {
    function updateCars() {
      const section = sectionRef.current;
      if (!section) return;

      const rect = section.getBoundingClientRect();
      const distance = section.offsetHeight - window.innerHeight;
      const scroll = distance <= 0 ? 1 : clamp(-rect.top / distance);

      const nextCars = ROADS.map((road, index) => {
        const path = pathRefs.current[index];
        const length = path ? path.getTotalLength() : 0;
        const progress = clamp((scroll - road.car.start) / (road.car.end - road.car.start));
        const point = path ? path.getPointAtLength(length * progress) : { x: 0, y: 0 };
        const directionProgress = progress >= 0.995 ? clamp(progress - 0.004) : clamp(progress + 0.004);
        const directionPoint = path ? path.getPointAtLength(length * directionProgress) : point;
        const angle =
          progress >= 0.995
            ? (Math.atan2(point.y - directionPoint.y, point.x - directionPoint.x) * 180) / Math.PI
            : (Math.atan2(directionPoint.y - point.y, directionPoint.x - point.x) * 180) / Math.PI;

        return {
          id: road.id,
          x: point.x,
          y: point.y,
          angle,
          visible: scroll >= road.car.start,
          color: road.car.color,
          accent: road.car.accent,
        };
      });

      setCars(nextCars);
    }

    updateCars();
    window.addEventListener('scroll', updateCars, { passive: true });
    window.addEventListener('resize', updateCars);
    return () => {
      window.removeEventListener('scroll', updateCars);
      window.removeEventListener('resize', updateCars);
    };
  }, [sectionRef]);

  return (
    <svg viewBox="0 0 1000 900" className="h-full w-full rounded-[2rem] bg-[#79a85c]">
      <rect width="1000" height="900" fill="#79a85c" />
      <path d="M0 130 C190 70 300 140 500 90 C700 40 850 130 1000 70 V0 H0 Z" fill="#6f9d52" />
      <path d="M0 780 C180 720 350 820 540 760 C720 700 830 805 1000 735 V900 H0 Z" fill="#6c994d" />

      {ROADS.map((road) => (
        <path key={`${road.id}-curb`} d={road.d} fill="none" stroke="#d9dddc" strokeWidth="90" strokeLinecap="round" strokeLinejoin="round" />
      ))}

      {ROADS.map((road, index) => (
        <path
          key={road.id}
          ref={(el) => { pathRefs.current[index] = el; }}
          d={road.d}
          fill="none"
          stroke="#252a30"
          strokeWidth="76"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      ))}

      {ROADS.map((road) => (
        <path key={`${road.id}-lane`} d={road.d} fill="none" stroke="white" strokeWidth="4" strokeDasharray="24 26" strokeLinecap="round" opacity="0.5" />
      ))}

      {cars.map((car) => (
        <Car key={car.id} car={car} />
      ))}
    </svg>
  );
}
