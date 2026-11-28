# OJunnySyS engineering standards

## Web interface

- Use Tailwind CSS utilities for layout, spacing, sizing, typography, color, borders, and responsive behavior. Avoid adding page- or component-specific CSS files.
- Keep custom CSS only for browser behavior that utilities cannot express cleanly, such as print rules, keyframe animation, third-party integration, or a documented complex reusable pattern.
- Before changing layout, inspect the parent and sibling structure and existing responsive behavior. Remove conflicting declarations instead of stacking overrides or using `!important`.
- Use Flexbox or CSS Grid for layout. Do not use floats, negative margins, or absolute positioning to arrange page content. Overlay affordances may use positioning only when they are semantically overlays.
- Apply `box-sizing: border-box` globally. Prefer intrinsic sizing, `min-*`, fractions, percentages, and relative units over fixed dimensions for content containers.
- Center content with Flexbox alignment or Grid `place-items`.
- Keep Tailwind classes statically detectable in Razor markup; do not construct utility names by concatenating strings.
- Keep UI labels and accessibility semantics in Razor; keep application behavior in the existing C# layers.

## Architecture

- Preserve the existing layered ASP.NET Core and Blazor architecture and its established naming conventions.
- Prefer shared Razor components and application services for reusable behavior instead of duplicating logic across pages.
- Do not introduce PHP into the solution.
