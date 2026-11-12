(() => {
  'use strict';
  const base = document.querySelector('meta[name="miko-base"]').content;
  const zh = document.documentElement.lang.toLowerCase().startsWith('zh');
  const t = (english, chinese) => zh ? chinese : english;
  const theme = document.getElementById('theme-toggle');
  const syncTheme = () => {
    const dark = document.documentElement.classList.contains('dark');
    theme?.setAttribute('aria-pressed', String(dark));
    theme?.setAttribute('aria-label', dark ? t('Switch to light theme', '切换浅色模式') : t('Switch to dark theme', '切换深色模式'));
  };
  syncTheme();
  theme?.addEventListener('click', () => {
    const dark = document.documentElement.classList.toggle('dark');
    try { localStorage.setItem('miko-theme', dark ? 'dark' : 'light'); } catch { /* Storage may be disabled. */ }
    syncTheme();
  });
  const menu = document.getElementById('mobile-nav');
  const toggle = document.getElementById('menu-toggle');
  const closeMenu = () => { menu.hidden = true; toggle.setAttribute('aria-expanded', 'false'); };
  toggle.addEventListener('click', () => {
    menu.hidden = !menu.hidden;
    toggle.setAttribute('aria-expanded', String(!menu.hidden));
  });
  menu.addEventListener('click', event => { if (event.target.closest('a')) closeMenu(); });
  matchMedia('(min-width: 960px)').addEventListener('change', event => { if (event.matches) closeMenu(); });
  document.addEventListener('keydown', event => { if (event.key === 'Escape') closeMenu(); });

  // Keep matching desktop and mobile groups in sync across full-page navigation.
  const groups = [...document.querySelectorAll('.VPSidebarItem[data-nav-key]')];
  const groupKey = group => {
    const path = [];
    for (let current = group; current; current = current.parentElement.closest('.VPSidebarItem[data-nav-key]')) path.unshift(current.dataset.navKey);
    return 'miko-sidebar:' + base + JSON.stringify(path);
  };
  for (const group of groups) {
    const key = groupKey(group);
    try {
      const saved = sessionStorage.getItem(key);
      if (saved !== null) group.open = saved === 'true';
    } catch { /* Storage may be disabled. */ }
    group.addEventListener('toggle', () => {
      try { sessionStorage.setItem(key, String(group.open)); } catch { /* Storage may be disabled. */ }
      for (const other of groups) if (other !== group && groupKey(other) === key && other.open !== group.open) other.open = group.open;
    });
  }
  const flyouts = [...document.querySelectorAll('.VPNavBarMenu > .VPNavBarMenuGroup')];
  for (const flyout of flyouts) {
    flyout.addEventListener('pointerenter', event => { if (event.pointerType === 'mouse') flyout.open = true; });
    flyout.addEventListener('pointerleave', event => { if (event.pointerType === 'mouse' && !flyout.contains(document.activeElement)) flyout.open = false; });
    flyout.addEventListener('focusout', event => { if (!flyout.contains(event.relatedTarget) && !flyout.matches(':hover')) flyout.open = false; });
  }
  document.addEventListener('click', event => { for (const flyout of flyouts) if (!flyout.contains(event.target)) flyout.open = false; });
  document.addEventListener('keydown', event => { if (event.key === 'Escape') for (const flyout of flyouts) flyout.open = false; });

  document.querySelectorAll('button.copy').forEach(button => {
    let timer;
    let pending = false;
    const label = text => { button.setAttribute('aria-label', text); button.title = text; };
    const reset = () => { button.classList.remove('copied'); label(t('Copy code', '复制代码')); };
    reset();
    button.addEventListener('click', async () => {
      if (pending) return;
      pending = true;
      clearTimeout(timer);
      reset();
      try {
        await navigator.clipboard.writeText(button.parentElement.querySelector('code').textContent);
        button.classList.add('copied');
        label(t('Copied', '已复制'));
      } catch {
        label(t('Copy failed; select the code manually', '复制失败，请手动选择代码'));
      } finally {
        pending = false;
        timer = setTimeout(reset, 2000);
      }
    });
  });
  if ('IntersectionObserver' in window) {
    const links = [...document.querySelectorAll('.outline-link')];
    const observer = new IntersectionObserver(entries => {
      for (const entry of entries) if (entry.isIntersecting) {
        for (const link of links) link.classList.toggle('active', decodeURIComponent(link.hash.slice(1)) === entry.target.id);
      }
    }, { rootMargin: '-80px 0px -65% 0px' });
    document.querySelectorAll('.vp-doc h2[id], .vp-doc h3[id]').forEach(heading => observer.observe(heading));
  }

  const dialog = document.getElementById('search-dialog');
  if (!dialog) return;
  const input = document.getElementById('search-input');
  const results = document.getElementById('search-results');
  const status = document.getElementById('search-status');
  let indexPromise;
  let generation = 0;
  function loadIndex() {
    if (!indexPromise) indexPromise = fetch(base + 'assets/search-index.json').then(response => {
      if (!response.ok) throw new Error('Search index unavailable');
      return response.json();
    }).catch(error => { indexPromise = null; throw error; });
    return indexPromise;
  }
  const open = () => { if (!dialog.open) dialog.showModal(); input.focus(); };
  document.getElementById('search-open').addEventListener('click', open);
  document.addEventListener('keydown', event => {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') { event.preventDefault(); open(); }
  });
  dialog.addEventListener('click', event => {
    const rect = dialog.getBoundingClientRect();
    if (event.target === dialog && (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom)) dialog.close();
  });
  input.addEventListener('input', async () => {
    const current = ++generation;
    const query = input.value.trim().toLocaleLowerCase();
    results.replaceChildren();
    if (!query) { status.textContent = t('Type to search the documentation', '输入关键词搜索文档'); return; }
    status.textContent = t('Searching…', '搜索中…');
    try {
      const index = await loadIndex();
      if (current !== generation) return;
      const terms = query.split(/\s+/);
      const matches = index.map(page => {
        const title = page.title.toLocaleLowerCase();
        const text = page.text.toLocaleLowerCase();
        return { ...page, score: terms.every(term => title.includes(term) || text.includes(term))
          ? terms.reduce((score, term) => score + (title.includes(term) ? 10 : 1), 0) : 0 };
      }).filter(page => page.score > 0).sort((a, b) => b.score - a.score).slice(0, 20);
      status.textContent = matches.length ? t(`Found ${matches.length} results`, `找到 ${matches.length} 条结果`) : t('No matching documents found', '没有找到匹配的文档');
      for (const page of matches) {
        const li = document.createElement('li'); const a = document.createElement('a');
        const title = document.createElement('strong'); const snippet = document.createElement('p');
        a.href = page.url; title.textContent = page.title;
        const at = Math.max(0, page.text.toLocaleLowerCase().indexOf(terms[0]) - 35);
        snippet.textContent = (at > 0 ? '…' : '') + page.text.slice(at, at + 160) + (page.text.length > at + 160 ? '…' : '');
        a.append(title, snippet); li.append(a); results.append(li);
      }
    } catch { if (current === generation) status.textContent = t('Search failed; type again to retry', '搜索加载失败，请重新输入以重试'); }
  });
  dialog.addEventListener('keydown', event => {
    if (event.key === 'Escape') { event.preventDefault(); dialog.close(); return; }
    const links = [...results.querySelectorAll('a')];
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      if (!links.length) return;
      event.preventDefault();
      const index = links.indexOf(document.activeElement);
      const next = event.key === 'ArrowDown' ? index + 1 : index - 1;
      if (next < 0) input.focus(); else links[Math.min(next, links.length - 1)].focus();
    } else if (event.key === 'Enter' && document.activeElement === input && links[0]) {
      event.preventDefault(); links[0].click();
    }
  });
})();
