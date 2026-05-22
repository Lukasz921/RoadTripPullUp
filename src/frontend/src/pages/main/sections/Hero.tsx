import { useRef } from 'react';
import SearchCard from '../components/SearchCard';
import RoadMap from '../components/RoadMap';

interface HeroProps {
  loggedIn: boolean;
}

export default function Hero({ loggedIn }: HeroProps) {
  const sectionRef = useRef<HTMLElement>(null);

  return (
    <section id="home" ref={sectionRef} className="min-h-[170vh] bg-[#12351f] px-6 pt-24">
      <div className="sticky top-24 flex min-h-[calc(100vh-6rem)] items-center justify-center">
        <div className="mx-auto grid w-full max-w-7xl items-center gap-8 lg:grid-cols-2">
          <SearchCard loggedIn={loggedIn} />
          <div className="aspect-square w-full rounded-[2.5rem] bg-[#77a75a] p-4 shadow-2xl shadow-black/30">
            <RoadMap sectionRef={sectionRef} />
          </div>
        </div>
      </div>
    </section>
  );
}
