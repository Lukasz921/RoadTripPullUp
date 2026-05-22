const ROUTE_PREVIEW_FIELDS = [
  { label: 'From', value: 'Warsaw' },
  { label: 'To', value: 'Kraków' },
  { label: 'Open seats', value: '3 seats' },
];

export default function AddRoute() {
  return (
    <section id="add-route" className="bg-[#eaf6df] px-6 py-28 text-[#12351f]">
      <div className="mx-auto grid max-w-7xl items-center gap-10 md:grid-cols-[1fr_0.9fr]">
        <div>
          <p className="text-sm font-black uppercase tracking-[0.25em] text-[#4f7f36]">For drivers</p>
          <h2 className="mt-4 text-5xl font-black leading-none md:text-7xl">Add your route and fill empty seats.</h2>
          <p className="mt-6 max-w-2xl text-lg leading-8 text-emerald-950/70">
            Driving somewhere? Publish your trip, set available seats, and let passengers join your trip.
          </p>
        </div>

        <div className="rounded-[2.5rem] bg-[#f7fbf0] p-8 shadow-2xl shadow-emerald-950/10">
          <div className="rounded-[2rem] bg-[#252a30] p-6 text-white">
            <p className="text-sm font-black uppercase tracking-[0.25em] text-[#8cc63f]">Route preview</p>
            <div className="mt-6 space-y-4">
              {ROUTE_PREVIEW_FIELDS.map((field) => (
                <div key={field.label} className="rounded-2xl bg-white/10 p-4">
                  <p className="text-sm text-white/50">{field.label}</p>
                  <p className="text-xl font-black">{field.value}</p>
                </div>
              ))}
            </div>
          </div>

          <a
            href="/add-route"
            className="mt-5 block rounded-2xl bg-[#252a30] px-8 py-5 text-center text-lg font-black text-white hover:bg-[#334155]"
          >
            Add route
          </a>
        </div>
      </div>
    </section>
  );
}
