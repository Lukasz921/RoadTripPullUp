import logoSrc from '../../assets/logo.svg';

export default function Navbar() {
  return (
    <header className="fixed inset-x-0 top-0 z-50 bg-[#12351f]/90 text-white backdrop-blur">
      <nav className="mx-auto flex h-20 max-w-7xl items-center justify-between px-6">
        <img src={logoSrc} className="h-12 w-36" alt="PullUp logo" />

        <div className="ml-auto flex gap-2">
          <button className="px-4 py-2 text-white/70 hover:text-white">Home</button>
          <button className="px-4 py-2 text-white/70 hover:text-white">Find ride</button>
          <button className="px-4 py-2 text-white/70 hover:text-white">Benefits</button>
          <button className="px-4 py-2 text-white/70 hover:text-white">Add route</button>
          <button className="px-4 py-2 text-white/70 hover:text-white">Login</button>
        </div>
      </nav>
    </header>
  );
}
