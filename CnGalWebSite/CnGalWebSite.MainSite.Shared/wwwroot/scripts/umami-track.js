/* 跨文件契约：TrackingMeta.razor 渲染 [data-cg-track-view]（data-cg-track-type/-id/-name）
   与 [data-cg-track-user-id]；点击埋点锚点带 a[data-cg-track-slot]（可选 data-cg-track-name） */
const maxQueueLength = 50;
const maxWaitCount = 100;

const queue = [];
let waitCount = 0;
let installed = false;
let lastIdentifiedId = null;
let lastNavMarkerKey = null;
let renderedPage = null;

function isReady() {
    return window.umami != null
        && typeof window.umami.track === 'function'
        && typeof window.umami.identify === 'function';
}

function snapshotPage() {
    return {
        url: location.origin + location.pathname + location.hash,
        title: document.title
    };
}

function truncate(value) {
    return value.length > 200 ? value.slice(0, 200) : value;
}

function sendEvent(name, data, page) {
    submit({ kind: 'event', name: name, data: data, page: page });
}

function sendIdentify(userId) {
    return submit({ kind: 'identify', id: userId });
}

function submit(item) {
    if (isReady()) {
        flushQueue();
        dispatch(item);
        return true;
    }
    if (waitCount < maxWaitCount && queue.length < maxQueueLength) {
        queue.push(item);
        return true;
    }
    return false;
}

function syncIdentity() {
    const element = document.querySelector('[data-cg-track-user-id]');
    const userId = element ? element.getAttribute('data-cg-track-user-id') : null;
    if (userId && userId !== lastIdentifiedId && sendIdentify(userId)) {
        lastIdentifiedId = userId;
    }
}

function sendContentView() {
    const element = document.querySelector('[data-cg-track-view]');
    const type = element ? element.getAttribute('data-cg-track-type') : null;
    const id = element ? element.getAttribute('data-cg-track-id') : null;
    if (!type || !id) {
        lastNavMarkerKey = null;
        return;
    }
    const key = type + '/' + id;
    if (key === lastNavMarkerKey) {
        return;
    }
    lastNavMarkerKey = key;
    const name = element.getAttribute('data-cg-track-name') || '';
    sendEvent('content_view', {
        type: type,
        id: id,
        name: truncate(name),
        path: location.pathname
    }, snapshotPage());
}

function deriveName(anchor) {
    const explicit = anchor.getAttribute('data-cg-track-name')
        || anchor.getAttribute('aria-label');
    if (explicit) {
        return truncate(explicit);
    }
    const image = anchor.querySelector('img[alt]');
    const alt = image ? image.getAttribute('alt') : null;
    if (alt) {
        return truncate(alt);
    }
    return truncate((anchor.textContent || '').trim());
}

function onClick(event) {
    const target = event.target instanceof Element ? event.target : null;
    const anchor = target ? target.closest('a[data-cg-track-slot]') : null;
    if (!anchor) {
        return;
    }
    const href = anchor.getAttribute('href') || '';
    const eventName = /^(https?:)?\/\//i.test(href) ? 'outbound_click' : 'promo_click';
    sendEvent(eventName, {
        slot: anchor.getAttribute('data-cg-track-slot'),
        url: truncate(href),
        name: deriveName(anchor)
    }, renderedPage);
}

function onPopState() {
    history.replaceState(history.state, '', location.href);
}

function dispatch(item) {
    try {
        let result;
        if (item.kind === 'identify') {
            result = window.umami.identify(item.id);
        } else {
            result = window.umami.track(function (p) {
                return { ...p, url: item.page.url, title: item.page.title, name: item.name, data: item.data };
            });
        }
        Promise.resolve(result).catch(function () {});
    } catch {
    }
}

function flushQueue() {
    while (queue.length > 0) {
        dispatch(queue.shift());
    }
}

function pollQueue() {
    if (isReady()) {
        flushQueue();
        return;
    }
    waitCount++;
    if (waitCount < maxWaitCount) {
        setTimeout(pollQueue, 100);
    } else {
        queue.length = 0;
        lastIdentifiedId = null;
    }
}

export function install() {
    if (installed) {
        return;
    }
    installed = true;
    renderedPage = snapshotPage();
    document.addEventListener('click', onClick, true);
    window.addEventListener('popstate', onPopState);
    syncIdentity();
    sendContentView();
    setTimeout(pollQueue, 100);
}

export function refresh() {
    renderedPage = snapshotPage();
    syncIdentity();
    sendContentView();
}
