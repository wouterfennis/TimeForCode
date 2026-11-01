# Expose a local container on the LAN

How to let another device on the same Wi-Fi reach a container that runs in Podman on a Windows development machine. The example uses the `pdfcraft` container (host port 8090); replace the name, port and IP for your own case.

Verified on Windows 11 with `podman machine` (WSL2 backend).

## Why it does not work by default

Podman on Windows forwards published ports to `127.0.0.1` on the host only, even when `podman ps` shows `0.0.0.0:<port>`. That mapping is inside the Podman machine. Two more things block other devices:

- Windows Firewall has no inbound rule for the port.
- Windows Firewall treats a Wi-Fi network classed as *Public* more strictly, so a rule limited to the *Private* profile does not apply to it.

## Steps

All commands run in an **elevated** PowerShell.

1. Check the container and what the host really listens on.

   ```powershell
   podman ps --filter name=pdfcraft --format "table {{.Names}}\t{{.Ports}}"
   netstat -an | findstr ":8090"
   ```

   `127.0.0.1:8090` as the only listener means other devices cannot connect yet.

1. Find the Wi-Fi IPv4 address (adapter `Wi-Fi`).

   ```powershell
   ipconfig
   ```

1. Forward the Wi-Fi address to the localhost listener. Listen on the Wi-Fi IP, not `0.0.0.0`, so the proxy does not clash with the existing localhost listener.

   ```powershell
   netsh interface portproxy add v4tov4 listenaddress=<wifi-ip> listenport=8090 connectaddress=127.0.0.1 connectport=8090
   ```

1. Allow the port through the firewall.

   ```powershell
   New-NetFirewallRule -DisplayName "pdfcraft" -Direction Inbound -Protocol TCP -LocalPort 8090 -Action Allow -Profile Private
   ```

1. Make sure the Wi-Fi network is classed as *Private* (only do this on a network you trust).

   ```powershell
   Get-NetConnectionProfile
   Set-NetConnectionProfile -InterfaceAlias "Wi-Fi" -NetworkCategory Private
   ```

1. Test from a phone or laptop on the same Wi-Fi with mobile data off: `http://<wifi-ip>:8090`.

   A request from the development machine to its own IP returns 200 even when the firewall blocks others, so it proves nothing.

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| Works on the dev machine, times out from other devices | Network is *Public* and the rule is *Private* only | Set the network to *Private*, or add `Public` to the rule's profile |
| Still times out with a correct rule | Router client isolation (AP isolation, usually on guest Wi-Fi) | Join the main network, or disable isolation in the router or its app |
| Worked yesterday, not today | DHCP gave the machine a new IP | Delete the old portproxy and add one for the new IP |
| Connection refused | Container stopped, or the host port changed | `podman ps`, then redo step 1 |

## Check and clean up

```powershell
netsh interface portproxy show v4tov4
Get-NetFirewallRule -DisplayName "pdfcraft" | Select-Object DisplayName, Enabled, Direction, Action, Profile
```

Remove the exposure when you are done:

```powershell
netsh interface portproxy delete v4tov4 listenaddress=<wifi-ip> listenport=8090
Remove-NetFirewallRule -DisplayName "pdfcraft"
```

## Security notes

- Anyone on the network can reach the service. Do not expose containers that have no authentication, hold secrets, or talk to real data.
- Switching a network to *Private* relaxes discovery and sharing for every service on that network. Switch it back afterwards if the network is not trusted.
- The TimeForCode services started by `scripts/start-local.ps1` use the same mechanism. Do not expose them with real credentials in the `.env` files.
