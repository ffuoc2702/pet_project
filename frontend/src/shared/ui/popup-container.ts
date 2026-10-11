export function getAppPopupContainer(trigger?: HTMLElement): HTMLElement {
  return (
    (trigger?.closest(
      '.ant-modal-container, .ant-drawer-section, .theme-root',
    ) as HTMLElement | null) ??
    trigger?.ownerDocument.body ??
    document.body
  );
}
