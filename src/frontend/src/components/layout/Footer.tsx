import logoSrc from '../../assets/logo.svg';

export default function Footer() {
  return (
    <footer className="bg-[#0b2115] px-6 py-10 text-white">
      <div className="mx-auto flex max-w-7xl flex-col gap-6 border-t border-white/10 pt-8 md:flex-row md:items-center md:justify-between">
        <div className="flex w-full flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <img src={logoSrc} className="h-12 w-36" alt="PullUp logo" />
          <p className="text-sm text-white/50 md:text-right">© 2026 PullUp™. All rights reserved.</p>
        </div>
      </div>
    </footer>
  );
}
