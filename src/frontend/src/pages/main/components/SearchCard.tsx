import StreetButton from '../../../components/ui/StreetButton';

export default function SearchCard() {
  return (
    <div className="flex aspect-square flex-col justify-between rounded-[2.5rem] bg-[#f7fbf0] p-8 text-[#12351f] shadow-2xl shadow-black/25">
      <div>
        <p className="text-sm font-black uppercase tracking-[0.25em] text-[#4f7f36]">PullUp</p>
        <h1 className="mt-4 text-5xl font-black leading-[0.92] md:text-7xl">Find a seat on your route.</h1>
        <p className="mt-5 text-lg leading-8 text-emerald-950/70">Search shared rides between cities.</p>
      </div>

      <div className="flex flex-col gap-4">
        <StreetButton href="/login">Log in</StreetButton>
        <StreetButton href="/register">Register</StreetButton>
      </div>
    </div>
  );
}
