# MatchingSports Figma build

Figma file: https://www.figma.com/design/Hy26nYnp9g5oj156rML1F7

## Current status

Figma MCP Starter quota stopped this run after **13 of 84 screens** were drawn: AUTH-01 through AUTH-08 and PROFILE-01 through PROFILE-05. Each has desktop 1440px and mobile 390px frames (**26 actual frames**). The other **71 screens / 142 frames are not yet in Figma**.

The native development plugin in this folder is prepared to finish the remaining screens in this exact file and connect prototype flows. **It has passed JavaScript syntax and inventory checks, but has not been run in Figma.** Running it remains necessary.

## Run the prepared plugin

1. Open the Figma file above in the **Figma desktop app**.
2. Open **Plugins → Development → Import plugin from manifest…** and select `manifest.json` from this folder.
3. Run **MatchingSports — Complete 84 Screens** and click **Dựng các màn còn thiếu**.
4. Review desktop/mobile layouts. Download the mapping and validation report using the plugin button.

The plugin uses no network requests and reuses the existing file's components, variables and sections. It skips screen frames that already exist and can be rerun after a failure. It never deletes existing nodes.

## Design scope

- 84 inventory screen IDs × desktop/mobile.
- Authentication, profiles, teams, listings, watches, matches, inbox, administration and common states.
- Native editable text and shared component instances; no UI screenshots used as fills.
- Light tokens and Ant Design visual conventions.
- Noto Sans is an explicit substitute for the source system-ui font stack unavailable to MCP.
- Sample names, times and addresses are illustration data.
- Modal/drawer backdrops, responsive cards instead of wide mobile tables, validation and permission states.
- Prototype links: authentication, listing request/approval/confirmation, watches/inbox, team invitations/requests and captaincy, admin notification composition/preview.
- Cross-page navigation is omitted because Figma prototype destinations stay in the same page.

## Artifacts

- `state.json`: completed canvas IDs and actual MCP status.
- `screen-specs.json`: content plan for all 84 screens.
- `code.js`, `ui.html`, `manifest.json`: native continuation plugin.
- `build-helper.js`: shared MCP construction helper.
- Source inventory retains metadata and receives IDs **only for actually created frames**.

## Verification

JavaScript syntax, 84-screen coverage, valid action destinations, manifest structure and completed mapping checked locally. The auth desktop/mobile composition was visually reviewed after sizing fixes. Backend code was unchanged; backend tests do not apply. Remaining Figma layouts and prototype execution cannot be claimed verified until the plugin is run and reviewed.
