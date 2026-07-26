# A framing belongs to a section, is relative to the page, and a page turn clears it

A pilot who zooms into the fuel block of a datacard, checks an approach plate and comes back expects
to find the fuel block where they left it, so a **framing** — how far in, and over which point of the
page — is remembered per section and restored on return. A page turn clears the section's framing back
to the fit: the pilot pages to read a new page whole, not to arrive at whatever corner of it the
previous page's framing happened to point at. A tap at either end of a section turns nothing, so it
clears nothing.

The framing is expressed relative to the page — a factor plus a centre as fractions of the page's
width and height — never as scroll offsets in pixels. Pixels were the cheaper option, since that is
what the platform view takes, but they close a hole rather than paper over it: zoom into one section,
switch to another, resize the window there, and pixel offsets captured against the old layout would
send you somewhere arbitrary on return. Page-relative framings make a resize a non-event, at the cost
of converting to and from scroll offsets in the platform handler.

## Consequences

Paging while zoomed still requires the pilot to be panned to an edge, because the page navigation
zones are a fifth of the *page* and slide out of the viewport as it magnifies. Restoring a zoomed
framing therefore makes it normal to arrive in a section where a tap on the margin does nothing until
the pilot double-taps back to the fit. That is accepted rather than overlooked: since a page turn
clears the framing anyway, "paging happens fitted" is one rule rather than two. Making the zones
screen-relative instead would fix it, and would undo the deliberately page-attached zones of #19.
