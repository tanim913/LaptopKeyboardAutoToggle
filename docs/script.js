/**
 * LaptopKeyboardAutoToggle — Landing Page & Simulator Logic
 * Impeccable craft implementation
 */

document.addEventListener('DOMContentLoaded', () => {
  initSimulator();
  initOsTabs();
  initCopyButtons();
});

/* ==========================================================================
   1. Interactive Hardware Simulator
   ========================================================================== */
function initSimulator() {
  const toggleBtn = document.getElementById('simToggleBtn');
  const btnLabel = document.getElementById('simBtnLabel');
  const dongleDot = document.getElementById('simDongleStatusDot');
  const dongleText = document.getElementById('simDongleStatusText');

  const laptopBox = document.getElementById('simLaptopBox');
  const laptopPill = document.getElementById('simLaptopPill');
  const laptopFrame = document.getElementById('simLaptopFrame');
  const laptopCaption = document.getElementById('simLaptopCaption');

  const wirelessBox = document.getElementById('simWirelessBox');
  const wirelessPill = document.getElementById('simWirelessPill');
  const wirelessFrame = document.getElementById('simWirelessFrame');
  const wirelessCaption = document.getElementById('simWirelessCaption');

  const consoleOutput = document.getElementById('simConsoleOutput');

  let isPluggedIn = true;

  function getTime() {
    const now = new Date();
    const pad = (n, z = 2) => String(n).padStart(z, '0');
    return `${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}.${pad(now.getMilliseconds(), 3)}`;
  }

  function appendLog(line) {
    if (!consoleOutput) return;
    const timeStr = getTime();
    consoleOutput.textContent += `[${timeStr}] ${line}\n`;
    consoleOutput.scrollTop = consoleOutput.scrollHeight;
  }

  // Initial state setup
  appendLog('Engine initialized: Interception driver loaded & ready');
  appendLog('Event listener active on WM_DEVICECHANGE & DBT_DEVTYP_DEVICEINTERFACE');
  appendLog('Hardware scan: Dongle VID_1EA7&PID_0066 detected');
  appendLog('Suppressing internal keyboard ACPI\\HPQ8002 (Interception filter active)');

  // Set initial muted styling for laptop keys
  if (laptopFrame) {
    laptopFrame.querySelectorAll('.key').forEach(k => k.classList.add('muted'));
    laptopBox.classList.add('muted-device');
  }

  toggleBtn.addEventListener('click', () => {
    isPluggedIn = !isPluggedIn;

    if (isPluggedIn) {
      // Plugged In State
      dongleDot.classList.remove('unplugged');
      dongleText.textContent = 'USB Dongle: Plugged In';
      btnLabel.textContent = 'Unplug USB Dongle';

      // Laptop keyboard muted
      laptopPill.textContent = 'MUTED';
      laptopPill.className = 'status-pill status-disabled';
      laptopCaption.textContent = 'Interception kernel filter is dropping all scancodes in memory before Windows sees them.';
      laptopBox.classList.add('muted-device');
      laptopBox.classList.remove('active-device');
      laptopFrame.querySelectorAll('.key').forEach(k => {
        k.classList.add('muted');
        k.classList.remove('active');
      });

      // Wireless keyboard connected
      wirelessPill.textContent = 'CONNECTED';
      wirelessPill.className = 'status-pill status-active';
      wirelessCaption.textContent = 'Keystrokes from VID_1EA7&PID_0066 pass through unmodified at full speed.';
      wirelessBox.classList.add('active-device');
      wirelessFrame.querySelectorAll('.key').forEach(k => k.classList.add('active'));

      // Console stream
      appendLog('WM_DEVICECHANGE: DBT_DEVICEARRIVAL');
      appendLog('Matched target device: VID_1EA7&PID_0066');
      appendLog('Activating filter for internal keyboard ACPI\\HPQ8002');
      appendLog('Zero-latency scancode suppression: ACTIVE');
    } else {
      // Unplugged State
      dongleDot.classList.add('unplugged');
      dongleText.textContent = 'USB Dongle: Unplugged';
      btnLabel.textContent = 'Plug in USB Dongle';

      // Laptop keyboard active
      laptopPill.textContent = 'ACTIVE';
      laptopPill.className = 'status-pill status-active';
      laptopCaption.textContent = 'Internal keyboard restored immediately. Keystrokes register normally.';
      laptopBox.classList.remove('muted-device');
      laptopBox.classList.add('active-device');
      laptopFrame.querySelectorAll('.key').forEach(k => {
        k.classList.remove('muted');
        k.classList.add('active');
      });

      // Wireless keyboard disconnected
      wirelessPill.textContent = 'OFFLINE';
      wirelessPill.className = 'status-pill status-offline';
      wirelessCaption.textContent = 'Wireless dongle removed from USB port.';
      wirelessBox.classList.remove('active-device');
      wirelessFrame.querySelectorAll('.key').forEach(k => k.classList.remove('active'));

      // Console stream
      appendLog('WM_DEVICECHANGE: DBT_DEVICEREMOVECOMPLETE');
      appendLog('Target dongle detached');
      appendLog('Deactivating Interception filter for ACPI\\HPQ8002');
      appendLog('Internal keyboard typing restored in 0.3ms');
    }
  });
}

/* ==========================================================================
   2. OS Switcher Tabs
   ========================================================================== */
function initOsTabs() {
  const tabs = document.querySelectorAll('.os-tab');
  const panes = {
    win: document.getElementById('pane-win'),
    linux: document.getElementById('pane-linux')
  };

  tabs.forEach(tab => {
    tab.addEventListener('click', () => {
      const target = tab.getAttribute('data-tab');

      // Update button active states
      tabs.forEach(t => t.classList.remove('active'));
      tab.classList.add('active');

      // Update pane visibility
      Object.keys(panes).forEach(key => {
        if (panes[key]) {
          if (key === target) {
            panes[key].classList.add('active');
          } else {
            panes[key].classList.remove('active');
          }
        }
      });
    });
  });
}

/* ==========================================================================
   3. Clipboard Copy Buttons
   ========================================================================== */
function initCopyButtons() {
  const copyButtons = document.querySelectorAll('.copy-btn');

  copyButtons.forEach(btn => {
    btn.addEventListener('click', async () => {
      const textToCopy = btn.getAttribute('data-copy');
      if (!textToCopy) return;

      try {
        await navigator.clipboard.writeText(textToCopy);
        const originalText = btn.textContent;
        btn.textContent = 'Copied!';
        btn.style.borderColor = 'var(--accent-emerald)';
        btn.style.color = 'var(--accent-emerald)';

        setTimeout(() => {
          btn.textContent = originalText;
          btn.style.borderColor = '';
          btn.style.color = '';
        }, 1800);
      } catch (err) {
        console.error('Copy failed:', err);
      }
    });
  });
}

