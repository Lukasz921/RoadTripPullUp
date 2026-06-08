const BENEFIT_CARDS = [
  {
    title: 'Save on gas',
    body: 'Share fuel costs with passengers and make each route much cheaper.',
  },
  {
    title: 'Talk during the ride',
    body: 'Chat with people riding with you before and during the trip.',
  },
  {
    title: 'Meet someone interesting',
    body: 'A shared route can become a good conversation, not just transport.',
  },
];

export default function Benefits() {
  return (
    <section id="benefits" className="bg-[#d7ebca] px-6 py-28 text-[#12351f]">
      <div className="mx-auto grid max-w-7xl items-center gap-10 md:grid-cols-[0.9fr_1.1fr]">
        <div>
          <p className="text-sm font-black uppercase tracking-[0.25em] text-[#4f7f36]">Why share a ride?</p>
          <h2 className="mt-4 text-5xl font-black leading-none md:text-7xl">Less gas. More people. Better trips.</h2>
          <p className="mt-6 max-w-2xl text-lg leading-8 text-emerald-950/70">
            PullUp helps people going the same way share one car instead of driving separately. Split the ride,
            save around 70% on gas, and turn empty seats into real conversations.
          </p>
        </div>

        <div className="grid gap-5">
          {BENEFIT_CARDS.map((card) => (
            <div key={card.title} className="rounded-[2rem] bg-[#f7fbf0] p-7 shadow-xl shadow-emerald-950/5">
              <h3 className="text-3xl font-black">{card.title}</h3>
              <p className="mt-3 text-zinc-600">{card.body}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
