import { useState } from 'react';
import { goTo } from '../../../utils/scroll';

interface SearchCardProps {
  loggedIn: boolean;
}

export default function SearchCard({ loggedIn }: SearchCardProps) {
  const [message, setMessage] = useState('');

  function search(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!loggedIn) {
      setMessage('Log in first to search rides.');
      goTo('login');
      return;
    }
    setMessage('Searching PullUp rides...');
  }

  return (
    <div className="flex aspect-square flex-col justify-between rounded-[2.5rem] bg-[#f7fbf0] p-8 text-[#12351f] shadow-2xl shadow-black/25">
      <div>
        <p className="text-sm font-black uppercase tracking-[0.25em] text-[#4f7f36]">PullUp</p>
        <h1 className="mt-4 text-5xl font-black leading-[0.92] md:text-7xl">Find a seat on your route.</h1>
        <p className="mt-5 text-lg leading-8 text-emerald-950/70">Search shared rides between cities.</p>
      </div>

      <form onSubmit={search} className="space-y-4">
        <div className="grid gap-4 sm:grid-cols-2">
          <input defaultValue="Warsaw" className="h-14 rounded-2xl border border-black/10 bg-white px-4 font-bold outline-none" />
          <input defaultValue="Kraków" className="h-14 rounded-2xl border border-black/10 bg-white px-4 font-bold outline-none" />
        </div>
        <input type="date" className="h-14 w-full rounded-2xl border border-black/10 bg-white px-4 font-bold outline-none" />
        <button className="h-16 w-full rounded-2xl bg-[#252a30] text-lg font-black text-white hover:bg-[#334155]">
          Search rides
        </button>
        {message && <p className="rounded-2xl bg-emerald-100 px-4 py-3 font-bold">{message}</p>}
      </form>
    </div>
  );
}
