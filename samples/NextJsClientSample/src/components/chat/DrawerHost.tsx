/**
 * DrawerHost - bridges context state to the RightDrawer component
 */

'use client';

import { useDrawer } from './DrawerContext';
import { RightDrawer } from './RightDrawer';

export function DrawerHost() {
  const drawer = useDrawer();
  return (
    <RightDrawer open={drawer.open} onClose={drawer.close} width={drawer.width}>
      {drawer.content}
    </RightDrawer>
  );
}


