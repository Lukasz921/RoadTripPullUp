import mapSrc from '../../../assets/map.png';
import StreetButton from '../../../components/ui/StreetButton';

export default function LoginCTA() {
  return (
    <section id="login" className="bg-[#12351f] px-6 py-28 text-white">
      <div className="mx-auto grid max-w-7xl items-center gap-10 md:grid-cols-[1.1fr_0.9fr]">
        <div>
          <p className="text-sm font-black uppercase tracking-[0.25em] text-[#8cc63f]">Join PullUp</p>
          <h2 className="mt-4 text-5xl font-black leading-none md:text-7xl">Ready to share the road?</h2>
          <p className="mt-6 max-w-2xl text-lg leading-8 text-white/70">
            Log in to search rides, or create a new account to start finding shared routes.
          </p>
          <div className="mt-8">
            <StreetButton href="/search" className="border border-white/40">Search rides</StreetButton>
          </div>
        </div>

        <div className="overflow-hidden rounded-2xl shadow-2xl">
          <img src={mapSrc} alt="Route map" className="w-full object-cover" />
        </div>
      </div>
    </section>
  );
}
