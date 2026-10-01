namespace AnsiDemo.Music;

/// <summary>
/// The demo's chiptune in the tracker's song format (spec 4.3): 125 bpm, 4 rows per beat, 2-bar patterns that
/// follow the scene boundaries of the demo script, plus a 2-bar ending so the song outlasts the picture.
/// </summary>
public static class Soundtrack
{
    public const string Text = """
        ; AnsiDemo soundtrack: A minor, Am F C G with a Dm E turnaround.
        ; Channels: 0 lead (pulse), 1 arpeggios, echo and harmony (pulse), 2 bass (triangle), 3 drums (noise).
        ; Every pattern is 2 bars (32 rows), so sections change exactly on the scene boundaries.

        bpm 125
        rows-per-beat 4

        instrument lead   duty=0.25  vol=0.55 decay=1.2
        instrument soft   duty=0.5   vol=0.4  decay=0.9
        instrument arp    duty=0.125 vol=0.4  decay=2.5
        instrument pad    duty=0.5   vol=0.3  decay=0.3
        instrument bass   duty=0.5   vol=0.9  decay=1.5
        instrument sub    duty=0.5   vol=0.8  decay=0.4
        instrument kick   duty=0.5   vol=0.8  decay=9
        instrument snare  duty=0.5   vol=0.6  decay=6
        instrument hat    duty=0.5   vol=0.3  decay=30

        ; boot: Am F, bass and quiet arps
        pattern intro1
        ---            | A-4 arp v5 a37 | A-2 sub        | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v3 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v5 a37 | ---            | C-8 hat v5
        ---            | ---            | ---            | ---
        ---            | A-4 arp v3 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v5 a37 | A-2 sub        | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v3 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v5 a37 | ---            | C-8 hat v5
        ---            | ---            | ---            | ---
        ---            | A-4 arp v3 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | F-4 arp v5 a47 | F-2 sub        | ---
        ---            | ---            | ---            | ---
        ---            | F-4 arp v3 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | F-4 arp v5 a47 | ---            | C-8 hat v5
        ---            | ---            | ---            | ---
        ---            | F-4 arp v3 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | F-4 arp v5 a47 | F-2 sub        | ---
        ---            | ---            | ---            | ---
        ---            | F-4 arp v3 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | F-4 arp v5 a47 | ---            | C-8 hat v5
        ---            | ---            | ---            | ---
        ---            | F-4 arp v3 a47 | ---            | ---
        ---            | ---            | ---            | ---
        end

        ; boot: C G, bass and quiet arps
        pattern intro2
        ---            | C-5 arp v5 a47 | C-3 sub        | ---
        ---            | ---            | ---            | ---
        ---            | C-5 arp v3 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | C-5 arp v5 a47 | ---            | C-8 hat v5
        ---            | ---            | ---            | ---
        ---            | C-5 arp v3 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | C-5 arp v5 a47 | C-3 sub        | ---
        ---            | ---            | ---            | ---
        ---            | C-5 arp v3 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | C-5 arp v5 a47 | ---            | C-8 hat v5
        ---            | ---            | ---            | ---
        ---            | C-5 arp v3 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | G-4 arp v5 a47 | G-2 sub        | ---
        ---            | ---            | ---            | ---
        ---            | G-4 arp v3 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | G-4 arp v5 a47 | ---            | C-8 hat v5
        ---            | ---            | ---            | ---
        ---            | G-4 arp v3 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | G-4 arp v5 a47 | G-2 sub        | ---
        ---            | ---            | ---            | ---
        ---            | G-4 arp v3 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | G-4 arp v5 a47 | ---            | C-8 hat v5
        ---            | ---            | ---            | ---
        ---            | G-4 arp v3 a47 | ---            | ---
        ---            | ---            | ---            | ---
        end

        ; boot: Am F, pad and kick join
        pattern intro3
        E-5 pad        | A-4 arp v7 a37 | A-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | A-4 arp v4 a37 | A-3 bass       | C-8 hat
        ---            | ---            | ---            | ---
        ---            | A-4 arp v7 a37 | A-2 bass       | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v4 a37 | A-3 bass       | C-8 hat
        ---            | ---            | ---            | ---
        ---            | A-4 arp v7 a37 | A-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | A-4 arp v4 a37 | A-3 bass       | C-8 hat
        ---            | ---            | ---            | ---
        ---            | A-4 arp v7 a37 | A-2 bass       | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v4 a37 | A-3 bass       | C-8 hat
        ---            | ---            | ---            | ---
        F-5 pad        | F-4 arp v7 a47 | F-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | F-4 arp v4 a47 | F-3 bass       | C-8 hat
        ---            | ---            | ---            | ---
        ---            | F-4 arp v7 a47 | F-2 bass       | ---
        ---            | ---            | ---            | ---
        ---            | F-4 arp v4 a47 | F-3 bass       | C-8 hat
        ---            | ---            | ---            | ---
        ---            | F-4 arp v7 a47 | F-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | F-4 arp v4 a47 | F-3 bass       | C-8 hat
        ---            | ---            | ---            | ---
        ---            | F-4 arp v7 a47 | F-2 bass       | ---
        ---            | ---            | ---            | ---
        ---            | F-4 arp v4 a47 | F-3 bass       | C-8 hat
        ---            | ---            | ---            | ---
        end

        ; boot: C G, pad and kick join
        pattern intro4
        G-5 pad        | C-5 arp v7 a47 | C-3 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | C-5 arp v4 a47 | C-4 bass       | C-8 hat
        ---            | ---            | ---            | ---
        ---            | C-5 arp v7 a47 | C-3 bass       | ---
        ---            | ---            | ---            | ---
        ---            | C-5 arp v4 a47 | C-4 bass       | C-8 hat
        ---            | ---            | ---            | ---
        ---            | C-5 arp v7 a47 | C-3 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | C-5 arp v4 a47 | C-4 bass       | C-8 hat
        ---            | ---            | ---            | ---
        ---            | C-5 arp v7 a47 | C-3 bass       | ---
        ---            | ---            | ---            | ---
        ---            | C-5 arp v4 a47 | C-4 bass       | C-8 hat
        ---            | ---            | ---            | ---
        D-5 pad        | G-4 arp v7 a47 | G-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | G-4 arp v4 a47 | G-3 bass       | C-8 hat
        ---            | ---            | ---            | ---
        ---            | G-4 arp v7 a47 | G-2 bass       | ---
        ---            | ---            | ---            | ---
        ---            | G-4 arp v4 a47 | G-3 bass       | C-8 hat
        ---            | ---            | ---            | ---
        ---            | G-4 arp v7 a47 | G-2 bass       | D-5 snare v2
        ---            | ---            | ---            | D-5 snare v3
        ---            | G-4 arp v4 a47 | G-3 bass       | D-5 snare v4
        ---            | ---            | ---            | D-5 snare v5
        ---            | G-4 arp v7 a47 | G-2 bass       | D-5 snare v6
        ---            | ---            | ---            | D-5 snare v7
        ---            | G-4 arp v4 a47 | G-3 bass       | D-5 snare v8
        ---            | ---            | ---            | D-5 snare v9
        end

        ; theme: Am F, lead with echo
        pattern mainA1
        E-5 lead       | ---            | A-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | ---            | A-3 bass       | C-8 hat
        E-5 lead       | E-5 soft v4    | ---            | ---
        ---            | ---            | A-2 bass       | D-5 snare
        ---            | ---            | ---            | ---
        D-5 lead       | E-5 soft v4    | A-3 bass       | C-8 hat
        ---            | ---            | ---            | C-8 hat v4
        C-5 lead       | ---            | A-2 bass       | C-2 kick
        ---            | D-5 soft v4    | ---            | ---
        D-5 lead       | ---            | A-3 bass       | C-2 kick
        ---            | C-5 soft v4    | ---            | ---
        E-5 lead       | ---            | A-2 bass       | D-5 snare
        ---            | D-5 soft v4    | ---            | ---
        ---            | ---            | A-3 bass       | C-8 hat
        ---            | E-5 soft v4    | ---            | C-8 hat v4
        F-5 lead       | ---            | F-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | ---            | F-3 bass       | C-8 hat
        ---            | F-5 soft v4    | ---            | ---
        E-5 lead       | ---            | F-2 bass       | D-5 snare
        ---            | ---            | ---            | ---
        C-5 lead       | ---            | F-3 bass       | C-8 hat
        ---            | E-5 soft v4    | ---            | C-8 hat v4
        A-4 lead       | ---            | F-2 bass       | C-2 kick
        ---            | C-5 soft v4    | ---            | ---
        ---            | ---            | F-3 bass       | C-2 kick
        ---            | A-4 soft v4    | ---            | ---
        ---            | ---            | F-2 bass       | D-5 snare
        ---            | ---            | ---            | ---
        off            | ---            | F-3 bass       | C-8 hat
        ---            | ---            | ---            | C-8 hat v4
        end

        ; theme: C G, lead with echo
        pattern mainA2
        G-5 lead       | ---            | C-3 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | ---            | C-4 bass       | C-8 hat
        G-5 lead       | G-5 soft v4    | ---            | ---
        ---            | ---            | C-3 bass       | D-5 snare
        ---            | ---            | ---            | ---
        F-5 lead       | G-5 soft v4    | C-4 bass       | C-8 hat
        ---            | ---            | ---            | C-8 hat v4
        E-5 lead       | ---            | C-3 bass       | C-2 kick
        ---            | F-5 soft v4    | ---            | ---
        D-5 lead       | ---            | C-4 bass       | C-2 kick
        ---            | E-5 soft v4    | ---            | ---
        E-5 lead       | ---            | C-3 bass       | D-5 snare
        ---            | D-5 soft v4    | ---            | ---
        ---            | ---            | C-4 bass       | C-8 hat
        ---            | E-5 soft v4    | ---            | C-8 hat v4
        D-5 lead       | ---            | G-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | ---            | G-3 bass       | C-8 hat
        ---            | D-5 soft v4    | ---            | ---
        B-4 lead       | ---            | G-2 bass       | D-5 snare
        ---            | ---            | ---            | ---
        G-4 lead       | ---            | G-3 bass       | C-8 hat
        ---            | B-4 soft v4    | ---            | C-8 hat v4
        D-5 lead       | ---            | G-2 bass       | C-2 kick
        ---            | G-4 soft v4    | ---            | ---
        ---            | ---            | G-3 bass       | C-2 kick
        ---            | D-5 soft v4    | ---            | ---
        ---            | ---            | G-2 bass       | D-5 snare
        ---            | ---            | ---            | ---
        off            | ---            | G-3 bass       | C-8 hat
        ---            | ---            | ---            | C-8 hat v4
        end

        ; theme: Am F, lead with echo
        pattern mainB1
        A-5 lead       | ---            | A-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | ---            | A-3 bass       | C-8 hat
        G-5 lead       | A-5 soft v4    | ---            | ---
        ---            | ---            | A-2 bass       | D-5 snare
        ---            | ---            | ---            | ---
        E-5 lead       | G-5 soft v4    | A-3 bass       | C-8 hat
        ---            | ---            | ---            | C-8 hat v4
        C-5 lead       | ---            | A-2 bass       | C-2 kick
        ---            | E-5 soft v4    | ---            | ---
        E-5 lead       | ---            | A-3 bass       | C-2 kick
        ---            | C-5 soft v4    | ---            | ---
        A-5 lead       | ---            | A-2 bass       | D-5 snare
        ---            | E-5 soft v4    | ---            | ---
        ---            | ---            | A-3 bass       | C-8 hat
        ---            | A-5 soft v4    | ---            | C-8 hat v4
        C-6 lead       | ---            | F-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | ---            | F-3 bass       | C-8 hat
        ---            | C-6 soft v4    | ---            | ---
        A-5 lead       | ---            | F-2 bass       | D-5 snare
        ---            | ---            | ---            | ---
        F-5 lead       | ---            | F-3 bass       | C-8 hat
        ---            | A-5 soft v4    | ---            | C-8 hat v4
        A-5 lead       | ---            | F-2 bass       | C-2 kick
        ---            | F-5 soft v4    | ---            | ---
        ---            | ---            | F-3 bass       | C-2 kick
        ---            | A-5 soft v4    | ---            | ---
        G-5 lead       | ---            | F-2 bass       | D-5 snare
        ---            | ---            | ---            | ---
        ---            | ---            | F-3 bass       | C-8 hat
        ---            | G-5 soft v4    | ---            | C-8 hat v4
        end

        ; theme: C G, lead with echo
        pattern mainB2
        E-5 lead       | ---            | C-3 bass       | C-2 kick
        ---            | ---            | ---            | ---
        G-5 lead       | ---            | C-4 bass       | C-8 hat
        ---            | E-5 soft v4    | ---            | ---
        C-6 lead       | ---            | C-3 bass       | D-5 snare
        ---            | G-5 soft v4    | ---            | ---
        ---            | ---            | C-4 bass       | C-8 hat
        ---            | C-6 soft v4    | ---            | C-8 hat v4
        B-5 lead       | ---            | C-3 bass       | C-2 kick
        ---            | ---            | ---            | ---
        G-5 lead       | ---            | C-4 bass       | C-2 kick
        ---            | B-5 soft v4    | ---            | ---
        E-5 lead       | ---            | C-3 bass       | D-5 snare
        ---            | G-5 soft v4    | ---            | ---
        ---            | ---            | C-4 bass       | C-8 hat
        ---            | E-5 soft v4    | ---            | C-8 hat v4
        D-5 lead       | ---            | G-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        G-5 lead       | ---            | G-3 bass       | C-8 hat
        ---            | D-5 soft v4    | ---            | ---
        B-5 lead       | ---            | G-2 bass       | D-5 snare
        ---            | G-5 soft v4    | ---            | ---
        ---            | ---            | G-3 bass       | C-8 hat
        ---            | B-5 soft v4    | ---            | C-8 hat v4
        A-5 lead       | ---            | G-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        G-5 lead       | ---            | G-3 bass       | C-2 kick
        ---            | A-5 soft v4    | ---            | ---
        D-5 lead       | ---            | G-2 bass       | D-5 snare
        ---            | G-5 soft v4    | ---            | ---
        ---            | ---            | G-3 bass       | C-8 hat
        ---            | D-5 soft v4    | ---            | C-8 hat v4
        end

        ; turnaround: Dm E, back to Am
        pattern turn
        F-5 lead       | ---            | D-3 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | ---            | D-4 bass       | C-8 hat
        E-5 lead       | F-5 soft v4    | ---            | ---
        ---            | ---            | D-3 bass       | D-5 snare
        ---            | ---            | ---            | ---
        D-5 lead       | E-5 soft v4    | D-4 bass       | C-8 hat
        ---            | ---            | ---            | C-8 hat v4
        A-4 lead       | ---            | D-3 bass       | C-2 kick
        ---            | D-5 soft v4    | ---            | ---
        ---            | ---            | D-4 bass       | C-2 kick
        ---            | A-4 soft v4    | ---            | ---
        D-5 lead       | ---            | D-3 bass       | D-5 snare
        ---            | ---            | ---            | ---
        ---            | ---            | D-4 bass       | C-8 hat
        ---            | D-5 soft v4    | ---            | C-8 hat v4
        E-5 lead       | ---            | E-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | ---            | E-3 bass       | ---
        ---            | E-5 soft v4    | ---            | ---
        G#5 lead       | ---            | E-2 bass       | D-5 snare
        ---            | ---            | ---            | ---
        ---            | ---            | E-3 bass       | ---
        ---            | G#5 soft v4    | ---            | ---
        B-5 lead       | ---            | E-2 bass       | C-2 kick
        ---            | ---            | ---            | ---
        ---            | ---            | E-3 bass       | C-8 hat
        ---            | B-5 soft v4    | ---            | ---
        E-6 lead       | ---            | E-2 bass       | D-5 snare v6
        ---            | ---            | ---            | D-5 snare v7
        ---            | ---            | E-3 bass       | D-5 snare v8
        ---            | E-6 soft v4    | ---            | D-5 snare v9
        end

        ; tunnel: Am F, arpeggios and counter melody
        pattern arpA1
        A-4 arp v9 a37 | C-5 soft v8    | A-2 bass       | C-2 kick
        A-4 arp v6 a37 | ---            | ---            | ---
        A-4 arp v6 a37 | ---            | A-3 bass       | C-8 hat
        A-4 arp v6 a37 | ---            | ---            | ---
        A-4 arp v9 a37 | ---            | A-2 bass       | D-5 snare
        A-4 arp v6 a37 | ---            | ---            | ---
        A-4 arp v6 a37 | ---            | A-3 bass       | C-8 hat
        A-4 arp v6 a37 | ---            | ---            | C-8 hat v4
        A-4 arp v9 a37 | E-5 soft v8    | A-2 bass       | C-2 kick
        A-4 arp v6 a37 | ---            | ---            | ---
        A-4 arp v6 a37 | ---            | A-3 bass       | C-2 kick
        A-4 arp v6 a37 | ---            | ---            | ---
        A-4 arp v9 a37 | D-5 soft v8    | A-2 bass       | D-5 snare
        A-4 arp v6 a37 | ---            | ---            | ---
        A-4 arp v6 a37 | ---            | A-3 bass       | C-8 hat
        A-4 arp v6 a37 | ---            | ---            | C-8 hat v4
        F-4 arp v9 a47 | C-5 soft v8    | F-2 bass       | C-2 kick
        F-4 arp v6 a47 | ---            | ---            | ---
        F-4 arp v6 a47 | ---            | F-3 bass       | C-8 hat
        F-4 arp v6 a47 | ---            | ---            | ---
        F-4 arp v9 a47 | ---            | F-2 bass       | D-5 snare
        F-4 arp v6 a47 | ---            | ---            | ---
        F-4 arp v6 a47 | ---            | F-3 bass       | C-8 hat
        F-4 arp v6 a47 | ---            | ---            | C-8 hat v4
        F-4 arp v9 a47 | A-4 soft v8    | F-2 bass       | C-2 kick
        F-4 arp v6 a47 | ---            | ---            | ---
        F-4 arp v6 a47 | ---            | F-3 bass       | C-2 kick
        F-4 arp v6 a47 | ---            | ---            | ---
        F-4 arp v9 a47 | C-5 soft v8    | F-2 bass       | D-5 snare
        F-4 arp v6 a47 | ---            | ---            | ---
        F-4 arp v6 a47 | ---            | F-3 bass       | C-8 hat
        F-4 arp v6 a47 | ---            | ---            | C-8 hat v4
        end

        ; tunnel: C G, arpeggios and counter melody
        pattern arpA2
        C-5 arp v9 a47 | E-5 soft v8    | C-3 bass       | C-2 kick
        C-5 arp v6 a47 | ---            | ---            | ---
        C-5 arp v6 a47 | ---            | C-4 bass       | C-8 hat
        C-5 arp v6 a47 | ---            | ---            | ---
        C-5 arp v9 a47 | ---            | C-3 bass       | D-5 snare
        C-5 arp v6 a47 | ---            | ---            | ---
        C-5 arp v6 a47 | ---            | C-4 bass       | C-8 hat
        C-5 arp v6 a47 | ---            | ---            | C-8 hat v4
        C-5 arp v9 a47 | G-5 soft v8    | C-3 bass       | C-2 kick
        C-5 arp v6 a47 | ---            | ---            | ---
        C-5 arp v6 a47 | ---            | C-4 bass       | C-2 kick
        C-5 arp v6 a47 | ---            | ---            | ---
        C-5 arp v9 a47 | F-5 soft v8    | C-3 bass       | D-5 snare
        C-5 arp v6 a47 | ---            | ---            | ---
        C-5 arp v6 a47 | ---            | C-4 bass       | C-8 hat
        C-5 arp v6 a47 | ---            | ---            | C-8 hat v4
        G-4 arp v9 a47 | D-5 soft v8    | G-2 bass       | C-2 kick
        G-4 arp v6 a47 | ---            | ---            | ---
        G-4 arp v6 a47 | ---            | G-3 bass       | C-8 hat
        G-4 arp v6 a47 | ---            | ---            | ---
        G-4 arp v9 a47 | ---            | G-2 bass       | D-5 snare
        G-4 arp v6 a47 | ---            | ---            | ---
        G-4 arp v6 a47 | ---            | G-3 bass       | C-8 hat
        G-4 arp v6 a47 | ---            | ---            | C-8 hat v4
        G-4 arp v9 a47 | B-4 soft v8    | G-2 bass       | C-2 kick
        G-4 arp v6 a47 | ---            | ---            | ---
        G-4 arp v6 a47 | ---            | G-3 bass       | C-2 kick
        G-4 arp v6 a47 | ---            | ---            | ---
        G-4 arp v9 a47 | D-5 soft v8    | G-2 bass       | D-5 snare
        G-4 arp v6 a47 | ---            | ---            | ---
        G-4 arp v6 a47 | ---            | G-3 bass       | C-8 hat
        G-4 arp v6 a47 | ---            | ---            | C-8 hat v4
        end

        ; fire: Am F, theme up an octave
        pattern fire1
        E-6 lead       | A-3 arp v6 a37 | A-2 bass       | C-2 kick
        ---            | ---            | A-2 bass v6    | C-8 hat v4
        ---            | A-3 arp v4 a37 | A-3 bass       | C-8 hat v7
        E-6 lead       | ---            | A-2 bass v6    | C-8 hat v4
        ---            | A-3 arp v6 a37 | A-2 bass       | D-5 snare
        ---            | ---            | A-2 bass v6    | C-8 hat v4
        D-6 lead       | A-3 arp v4 a37 | A-3 bass       | C-8 hat v7
        ---            | ---            | A-2 bass v6    | C-8 hat v4
        C-6 lead       | A-3 arp v6 a37 | A-2 bass       | C-2 kick
        ---            | ---            | A-2 bass v6    | C-8 hat v4
        D-6 lead       | A-3 arp v4 a37 | A-3 bass       | C-8 hat v7
        ---            | ---            | A-2 bass v6    | C-8 hat v4
        E-6 lead       | A-3 arp v6 a37 | A-2 bass       | D-5 snare
        ---            | ---            | A-2 bass v6    | C-8 hat v4
        ---            | A-3 arp v4 a37 | A-3 bass       | C-8 hat v7
        ---            | ---            | A-2 bass v6    | C-8 hat v4
        F-6 lead       | F-3 arp v6 a47 | F-2 bass       | C-2 kick
        ---            | ---            | F-2 bass v6    | C-8 hat v4
        ---            | F-3 arp v4 a47 | F-3 bass       | C-8 hat v7
        ---            | ---            | F-2 bass v6    | C-8 hat v4
        E-6 lead       | F-3 arp v6 a47 | F-2 bass       | D-5 snare
        ---            | ---            | F-2 bass v6    | C-8 hat v4
        C-6 lead       | F-3 arp v4 a47 | F-3 bass       | C-8 hat v7
        ---            | ---            | F-2 bass v6    | C-8 hat v4
        A-5 lead       | F-3 arp v6 a47 | F-2 bass       | C-2 kick
        ---            | ---            | F-2 bass v6    | C-8 hat v4
        ---            | F-3 arp v4 a47 | F-3 bass       | C-8 hat v7
        ---            | ---            | F-2 bass v6    | C-8 hat v4
        ---            | F-3 arp v6 a47 | F-2 bass       | D-5 snare
        ---            | ---            | F-2 bass v6    | C-8 hat v4
        off            | F-3 arp v4 a47 | F-3 bass       | C-8 hat v7
        ---            | ---            | F-2 bass v6    | C-8 hat v4
        end

        ; fire: C G, theme up an octave
        pattern fire2
        G-6 lead       | C-4 arp v6 a47 | C-3 bass       | C-2 kick
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        ---            | C-4 arp v4 a47 | C-4 bass       | C-8 hat v7
        G-6 lead       | ---            | C-3 bass v6    | C-8 hat v4
        ---            | C-4 arp v6 a47 | C-3 bass       | D-5 snare
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        F-6 lead       | C-4 arp v4 a47 | C-4 bass       | C-8 hat v7
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        E-6 lead       | C-4 arp v6 a47 | C-3 bass       | C-2 kick
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        D-6 lead       | C-4 arp v4 a47 | C-4 bass       | C-8 hat v7
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        E-6 lead       | C-4 arp v6 a47 | C-3 bass       | D-5 snare
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        ---            | C-4 arp v4 a47 | C-4 bass       | C-8 hat v7
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        D-6 lead       | G-3 arp v6 a47 | G-2 bass       | C-2 kick
        ---            | ---            | G-2 bass v6    | C-8 hat v4
        ---            | G-3 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | ---            | G-2 bass v6    | C-8 hat v4
        B-5 lead       | G-3 arp v6 a47 | G-2 bass       | D-5 snare
        ---            | ---            | G-2 bass v6    | C-8 hat v4
        G-5 lead       | G-3 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | ---            | G-2 bass v6    | C-8 hat v4
        D-6 lead       | G-3 arp v6 a47 | G-2 bass       | C-2 kick
        ---            | ---            | G-2 bass v6    | C-8 hat v4
        ---            | G-3 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | ---            | G-2 bass v6    | C-8 hat v4
        ---            | G-3 arp v6 a47 | G-2 bass       | D-5 snare
        ---            | ---            | G-2 bass v6    | C-8 hat v4
        off            | G-3 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | ---            | G-2 bass v6    | C-8 hat v4
        end

        ; fire: C G, theme up an octave, snare fill
        pattern fire3
        G-6 lead       | C-4 arp v6 a47 | C-3 bass       | C-2 kick
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        ---            | C-4 arp v4 a47 | C-4 bass       | C-8 hat v7
        G-6 lead       | ---            | C-3 bass v6    | C-8 hat v4
        ---            | C-4 arp v6 a47 | C-3 bass       | D-5 snare
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        F-6 lead       | C-4 arp v4 a47 | C-4 bass       | C-8 hat v7
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        E-6 lead       | C-4 arp v6 a47 | C-3 bass       | C-2 kick
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        D-6 lead       | C-4 arp v4 a47 | C-4 bass       | C-8 hat v7
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        E-6 lead       | C-4 arp v6 a47 | C-3 bass       | D-5 snare
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        ---            | C-4 arp v4 a47 | C-4 bass       | C-8 hat v7
        ---            | ---            | C-3 bass v6    | C-8 hat v4
        D-6 lead       | G-3 arp v6 a47 | G-2 bass       | C-2 kick
        ---            | ---            | G-2 bass v6    | C-8 hat v4
        ---            | G-3 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | ---            | G-2 bass v6    | C-8 hat v4
        B-5 lead       | G-3 arp v6 a47 | G-2 bass       | D-5 snare
        ---            | ---            | G-2 bass v6    | C-8 hat v4
        G-5 lead       | G-3 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | ---            | G-2 bass v6    | C-8 hat v4
        D-6 lead       | G-3 arp v6 a47 | G-2 bass       | D-5 snare v6
        ---            | ---            | G-2 bass v6    | D-5 snare v7
        ---            | G-3 arp v4 a47 | G-3 bass       | D-5 snare v8
        ---            | ---            | G-2 bass v6    | D-5 snare v9
        ---            | G-3 arp v6 a47 | G-2 bass       | D-5 snare v9
        ---            | ---            | G-2 bass v6    | D-5 snare v9
        off            | G-3 arp v4 a47 | G-3 bass       | D-5 snare v9
        ---            | ---            | G-2 bass v6    | D-5 snare v9
        end

        ; flight: Am F, theme with harmony, half-time
        pattern flight1
        A-5 lead       | F-5 soft v7    | A-2 bass       | C-2 kick
        ---            | ---            | ---            | C-8 hat v3
        ---            | ---            | ---            | C-8 hat v6
        G-5 lead       | E-5 soft v7    | ---            | C-8 hat v3
        ---            | ---            | ---            | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        E-5 lead       | C-5 soft v7    | A-2 bass       | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        C-5 lead       | A-4 soft v7    | A-2 bass       | D-5 snare
        ---            | ---            | ---            | C-8 hat v3
        E-5 lead       | C-5 soft v7    | ---            | C-2 kick v6
        ---            | ---            | ---            | C-8 hat v3
        A-5 lead       | F-5 soft v7    | A-2 bass       | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        ---            | ---            | ---            | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        C-6 lead       | A-5 soft v7    | F-2 bass       | C-2 kick
        ---            | ---            | ---            | C-8 hat v3
        ---            | ---            | ---            | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        A-5 lead       | F-5 soft v7    | ---            | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        F-5 lead       | D-5 soft v7    | F-2 bass       | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        A-5 lead       | F-5 soft v7    | F-2 bass       | D-5 snare
        ---            | ---            | ---            | C-8 hat v3
        ---            | ---            | ---            | C-2 kick v6
        ---            | ---            | ---            | C-8 hat v3
        G-5 lead       | E-5 soft v7    | F-2 bass       | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        ---            | ---            | ---            | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        end

        ; flight: C G, theme with harmony, half-time
        pattern flight2
        E-5 lead       | C-5 soft v7    | C-3 bass       | C-2 kick
        ---            | ---            | ---            | C-8 hat v3
        G-5 lead       | E-5 soft v7    | ---            | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        C-6 lead       | A-5 soft v7    | ---            | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        ---            | ---            | C-3 bass       | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        B-5 lead       | G-5 soft v7    | C-3 bass       | D-5 snare
        ---            | ---            | ---            | C-8 hat v3
        G-5 lead       | E-5 soft v7    | ---            | C-2 kick v6
        ---            | ---            | ---            | C-8 hat v3
        E-5 lead       | C-5 soft v7    | C-3 bass       | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        ---            | ---            | ---            | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        D-5 lead       | B-4 soft v7    | G-2 bass       | C-2 kick
        ---            | ---            | ---            | C-8 hat v3
        G-5 lead       | E-5 soft v7    | ---            | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        B-5 lead       | G-5 soft v7    | ---            | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        ---            | ---            | G-2 bass       | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        A-5 lead       | F-5 soft v7    | G-2 bass       | D-5 snare
        ---            | ---            | ---            | C-8 hat v3
        G-5 lead       | E-5 soft v7    | ---            | C-2 kick v6
        ---            | ---            | ---            | C-8 hat v3
        D-5 lead       | B-4 soft v7    | G-2 bass       | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        ---            | ---            | ---            | C-8 hat v6
        ---            | ---            | ---            | C-8 hat v3
        end

        ; breakdown: Am F, pads only
        pattern brk1
        A-4 pad        | A-4 arp v4 a37 | A-2 sub        | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v2 a37 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        ---            | A-4 arp v2 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v2 a37 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        E-5 pad        | A-4 arp v4 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v2 a37 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        ---            | A-4 arp v2 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v2 a37 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        F-5 pad        | F-4 arp v4 a47 | F-2 sub        | ---
        ---            | ---            | ---            | ---
        ---            | F-4 arp v2 a47 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        ---            | F-4 arp v2 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | F-4 arp v2 a47 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        C-5 pad        | F-4 arp v4 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | F-4 arp v2 a47 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        ---            | F-4 arp v2 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | F-4 arp v2 a47 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        end

        ; breakdown: C G, pads only
        pattern brk2
        E-5 pad        | C-5 arp v4 a47 | C-3 sub        | ---
        ---            | ---            | ---            | ---
        ---            | C-5 arp v2 a47 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        ---            | C-5 arp v2 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | C-5 arp v2 a47 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        G-5 pad        | C-5 arp v4 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | C-5 arp v2 a47 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        ---            | C-5 arp v2 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | C-5 arp v2 a47 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        D-5 pad        | G-4 arp v4 a47 | G-2 sub        | ---
        ---            | ---            | ---            | ---
        ---            | G-4 arp v2 a47 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        ---            | G-4 arp v2 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | G-4 arp v2 a47 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        B-4 pad        | G-4 arp v4 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | G-4 arp v2 a47 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        ---            | G-4 arp v2 a47 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | G-4 arp v2 a47 | ---            | C-8 hat v4
        ---            | ---            | ---            | ---
        end

        ; build-up: Dm E, rising arpeggios and snare roll
        pattern build
        F-5 soft       | D-5 arp v2 a37 | D-3 bass       | D-5 snare v4
        ---            | D-5 arp v2 a37 | ---            | ---
        ---            | D-5 arp v2 a37 | D-4 bass       | ---
        E-5 soft       | D-5 arp v2 a37 | ---            | ---
        ---            | D-5 arp v2 a37 | D-3 bass       | D-5 snare v4
        ---            | D-5 arp v3 a37 | ---            | ---
        D-5 soft       | D-5 arp v3 a37 | D-4 bass       | ---
        ---            | D-5 arp v3 a37 | ---            | ---
        A-4 soft       | D-5 arp v3 a37 | D-3 bass       | D-5 snare v4
        ---            | D-5 arp v4 a37 | ---            | ---
        ---            | D-5 arp v4 a37 | D-4 bass       | ---
        ---            | D-5 arp v4 a37 | ---            | ---
        D-5 soft       | D-5 arp v4 a37 | D-3 bass       | D-5 snare v4
        ---            | D-5 arp v4 a37 | ---            | ---
        ---            | D-5 arp v5 a37 | D-4 bass       | ---
        ---            | D-5 arp v5 a37 | ---            | ---
        E-5 soft       | E-4 arp v5 a47 | E-2 bass       | D-5 snare v6
        ---            | E-4 arp v5 a47 | ---            | ---
        ---            | E-4 arp v6 a47 | E-3 bass       | D-5 snare v6
        ---            | E-4 arp v6 a47 | ---            | ---
        G#5 soft       | E-4 arp v6 a47 | E-2 bass       | D-5 snare v6
        ---            | E-4 arp v6 a47 | ---            | ---
        ---            | E-4 arp v6 a47 | E-3 bass       | D-5 snare v6
        ---            | E-4 arp v7 a47 | ---            | ---
        B-5 soft       | E-4 arp v7 a47 | E-2 bass       | D-5 snare v6
        ---            | E-4 arp v7 a47 | ---            | D-5 snare v6
        ---            | E-4 arp v7 a47 | E-3 bass       | D-5 snare v7
        ---            | E-4 arp v8 a47 | ---            | D-5 snare v7
        E-6 soft       | E-4 arp v8 a47 | E-2 bass       | D-5 snare v8
        ---            | E-4 arp v8 a47 | ---            | D-5 snare v8
        ---            | E-4 arp v8 a47 | E-3 bass       | D-5 snare v9
        ---            | E-4 arp v9 a47 | ---            | D-5 snare v9
        end

        ; finale: Am F, theme over arpeggios
        pattern fin1
        E-5 lead       | A-4 arp v7 a37 | A-2 bass       | C-2 kick
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        ---            | A-4 arp v4 a37 | A-3 bass       | C-8 hat v7
        E-5 lead       | A-4 arp v4 a37 | ---            | C-8 hat v4
        ---            | A-4 arp v7 a37 | A-2 bass       | D-5 snare
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        D-5 lead       | A-4 arp v4 a37 | A-3 bass       | C-8 hat v7
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        C-5 lead       | A-4 arp v7 a37 | A-2 bass       | C-2 kick
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        D-5 lead       | A-4 arp v4 a37 | A-3 bass       | C-8 hat v7
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        E-5 lead       | A-4 arp v7 a37 | A-2 bass       | D-5 snare
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        ---            | A-4 arp v4 a37 | A-3 bass       | C-8 hat v7
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        F-5 lead       | F-4 arp v7 a47 | F-2 bass       | C-2 kick
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        ---            | F-4 arp v4 a47 | F-3 bass       | C-8 hat v7
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        E-5 lead       | F-4 arp v7 a47 | F-2 bass       | D-5 snare
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        C-5 lead       | F-4 arp v4 a47 | F-3 bass       | C-8 hat v7
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        A-4 lead       | F-4 arp v7 a47 | F-2 bass       | C-2 kick
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        ---            | F-4 arp v4 a47 | F-3 bass       | C-8 hat v7
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        ---            | F-4 arp v7 a47 | F-2 bass       | D-5 snare
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        off            | F-4 arp v4 a47 | F-3 bass       | C-8 hat v7
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        end

        ; finale: C G, theme over arpeggios
        pattern fin2
        G-5 lead       | C-5 arp v7 a47 | C-3 bass       | C-2 kick
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        ---            | C-5 arp v4 a47 | C-4 bass       | C-8 hat v7
        G-5 lead       | C-5 arp v4 a47 | ---            | C-8 hat v4
        ---            | C-5 arp v7 a47 | C-3 bass       | D-5 snare
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        F-5 lead       | C-5 arp v4 a47 | C-4 bass       | C-8 hat v7
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        E-5 lead       | C-5 arp v7 a47 | C-3 bass       | C-2 kick
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        D-5 lead       | C-5 arp v4 a47 | C-4 bass       | C-8 hat v7
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        E-5 lead       | C-5 arp v7 a47 | C-3 bass       | D-5 snare
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        ---            | C-5 arp v4 a47 | C-4 bass       | C-8 hat v7
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        D-5 lead       | G-4 arp v7 a47 | G-2 bass       | C-2 kick
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        ---            | G-4 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        B-4 lead       | G-4 arp v7 a47 | G-2 bass       | D-5 snare
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        G-4 lead       | G-4 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        D-5 lead       | G-4 arp v7 a47 | G-2 bass       | C-2 kick
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        ---            | G-4 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        ---            | G-4 arp v7 a47 | G-2 bass       | D-5 snare
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        off            | G-4 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        end

        ; finale: Am F, theme over arpeggios
        pattern fin3
        A-5 lead       | A-4 arp v7 a37 | A-2 bass       | C-2 kick
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        ---            | A-4 arp v4 a37 | A-3 bass       | C-8 hat v7
        G-5 lead       | A-4 arp v4 a37 | ---            | C-8 hat v4
        ---            | A-4 arp v7 a37 | A-2 bass       | D-5 snare
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        E-5 lead       | A-4 arp v4 a37 | A-3 bass       | C-8 hat v7
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        C-5 lead       | A-4 arp v7 a37 | A-2 bass       | C-2 kick
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        E-5 lead       | A-4 arp v4 a37 | A-3 bass       | C-8 hat v7
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        A-5 lead       | A-4 arp v7 a37 | A-2 bass       | D-5 snare
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        ---            | A-4 arp v4 a37 | A-3 bass       | C-8 hat v7
        ---            | A-4 arp v4 a37 | ---            | C-8 hat v4
        C-6 lead       | F-4 arp v7 a47 | F-2 bass       | C-2 kick
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        ---            | F-4 arp v4 a47 | F-3 bass       | C-8 hat v7
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        A-5 lead       | F-4 arp v7 a47 | F-2 bass       | D-5 snare
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        F-5 lead       | F-4 arp v4 a47 | F-3 bass       | C-8 hat v7
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        A-5 lead       | F-4 arp v7 a47 | F-2 bass       | C-2 kick
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        ---            | F-4 arp v4 a47 | F-3 bass       | C-8 hat v7
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        G-5 lead       | F-4 arp v7 a47 | F-2 bass       | D-5 snare
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        ---            | F-4 arp v4 a47 | F-3 bass       | C-8 hat v7
        ---            | F-4 arp v4 a47 | ---            | C-8 hat v4
        end

        ; finale: C G, theme over arpeggios
        pattern fin4
        E-5 lead       | C-5 arp v7 a47 | C-3 bass       | C-2 kick
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        G-5 lead       | C-5 arp v4 a47 | C-4 bass       | C-8 hat v7
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        C-6 lead       | C-5 arp v7 a47 | C-3 bass       | D-5 snare
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        ---            | C-5 arp v4 a47 | C-4 bass       | C-8 hat v7
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        B-5 lead       | C-5 arp v7 a47 | C-3 bass       | C-2 kick
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        G-5 lead       | C-5 arp v4 a47 | C-4 bass       | C-8 hat v7
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        E-5 lead       | C-5 arp v7 a47 | C-3 bass       | D-5 snare
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        ---            | C-5 arp v4 a47 | C-4 bass       | C-8 hat v7
        ---            | C-5 arp v4 a47 | ---            | C-8 hat v4
        D-5 lead       | G-4 arp v7 a47 | G-2 bass       | C-2 kick
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        G-5 lead       | G-4 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        B-5 lead       | G-4 arp v7 a47 | G-2 bass       | D-5 snare
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        ---            | G-4 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        A-5 lead       | G-4 arp v7 a47 | G-2 bass       | C-2 kick
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        G-5 lead       | G-4 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        D-5 lead       | G-4 arp v7 a47 | G-2 bass       | D-5 snare
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        ---            | G-4 arp v4 a47 | G-3 bass       | C-8 hat v7
        ---            | G-4 arp v4 a47 | ---            | C-8 hat v4
        end

        ; finale turnaround: Dm E with a long fill
        pattern finturn
        F-5 lead       | D-5 arp v7 a37 | D-3 bass       | C-2 kick
        ---            | D-5 arp v4 a37 | D-3 bass v6    | C-8 hat v4
        ---            | D-5 arp v4 a37 | D-4 bass       | C-8 hat v7
        E-5 lead       | D-5 arp v4 a37 | D-3 bass v6    | C-8 hat v4
        ---            | D-5 arp v7 a37 | D-3 bass       | D-5 snare
        ---            | D-5 arp v4 a37 | D-3 bass v6    | C-8 hat v4
        D-5 lead       | D-5 arp v4 a37 | D-4 bass       | C-8 hat v7
        ---            | D-5 arp v4 a37 | D-3 bass v6    | C-8 hat v4
        A-4 lead       | D-5 arp v7 a37 | D-3 bass       | C-2 kick
        ---            | D-5 arp v4 a37 | D-3 bass v6    | C-8 hat v4
        ---            | D-5 arp v4 a37 | D-4 bass       | C-8 hat v7
        ---            | D-5 arp v4 a37 | D-3 bass v6    | C-8 hat v4
        D-5 lead       | D-5 arp v7 a37 | D-3 bass       | D-5 snare
        ---            | D-5 arp v4 a37 | D-3 bass v6    | C-8 hat v4
        ---            | D-5 arp v4 a37 | D-4 bass       | C-8 hat v7
        ---            | D-5 arp v4 a37 | D-3 bass v6    | C-8 hat v4
        E-5 lead       | E-4 arp v7 a47 | E-2 bass       | D-5 snare v5
        ---            | E-4 arp v4 a47 | E-2 bass v6    | ---
        ---            | E-4 arp v4 a47 | E-3 bass       | D-5 snare v5
        ---            | E-4 arp v4 a47 | E-2 bass v6    | ---
        G#5 lead       | E-4 arp v7 a47 | E-2 bass       | D-5 snare v6
        ---            | E-4 arp v4 a47 | E-2 bass v6    | ---
        ---            | E-4 arp v4 a47 | E-3 bass       | D-5 snare v6
        ---            | E-4 arp v4 a47 | E-2 bass v6    | ---
        B-5 lead       | E-4 arp v7 a47 | E-2 bass       | D-5 snare v7
        ---            | E-4 arp v4 a47 | E-2 bass v6    | ---
        ---            | E-4 arp v4 a47 | E-3 bass       | D-5 snare v7
        ---            | E-4 arp v4 a47 | E-2 bass v6    | ---
        E-6 lead       | E-4 arp v7 a47 | E-2 bass       | D-5 snare v6
        ---            | E-4 arp v4 a47 | E-2 bass v6    | D-5 snare v7
        ---            | E-4 arp v4 a47 | E-3 bass       | D-5 snare v8
        ---            | E-4 arp v4 a47 | E-2 bass v6    | D-5 snare v9
        end

        ; ending: final Am chord rings out
        pattern ending
        A-5 lead       | A-4 arp v9 a37 | A-2 sub        | C-2 kick
        ---            | ---            | ---            | ---
        ---            | A-4 arp v9 a37 | ---            | D-5 snare v5
        ---            | ---            | ---            | ---
        ---            | A-4 arp v8 a37 | ---            | C-8 hat v3
        ---            | ---            | ---            | ---
        ---            | A-4 arp v7 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v7 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v6 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v5 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v5 a37 | ---            | ---
        ---            | ---            | ---            | ---
        off            | A-4 arp v4 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v3 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v3 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | A-4 arp v2 a37 | ---            | ---
        ---            | ---            | ---            | ---
        ---            | off            | ---            | ---
        ---            | ---            | ---            | ---
        ---            | ---            | ---            | ---
        ---            | ---            | ---            | ---
        ---            | ---            | off            | ---
        ---            | ---            | ---            | ---
        ---            | ---            | ---            | ---
        ---            | ---            | ---            | ---
        end

        order intro1 intro2 intro3 intro4              ; boot, bars 0-7
        order mainA1 mainA2 mainB1 mainB2 turn         ; title, bars 8-17
        order arpA1 arpA2 arpA1 arpA2 turn             ; tunnel, bars 18-27
        order fire1 fire2 fire1 fire3                  ; fire, bars 28-35
        order mainA1 mainA2 mainB1 mainB2              ; roto, bars 36-43
        order flight1 flight2 flight1 flight2 turn     ; flight, bars 44-53
        order brk1 brk2 brk1 brk2 build                ; mandel, bars 54-63
        order fin1 fin2 fin3 fin4 finturn              ; spheres, bars 64-73
        order ending                                   ; ending, bars 74-75: keeps the song longer than the demo
        """;
}
