# Bundled word lists

Used by the layout corrector to decide which language the user *meant* to type:
a word is only converted when it is a real word in the target language and not a
real word in the language they typed it in.

Neither list is derived from any other keyboard-layout tool.

## `words_th.txt.gz` — 60,537 Thai words

Source: [PyThaiNLP](https://github.com/PyThaiNLP/pythainlp) `pythainlp/corpus/words_th.txt`.

Licence: **CC0 1.0 Universal (Public Domain Dedication)** — per PyThaiNLP's
`corpus_license.md`, word lists created by the project are released under CC0.
<https://creativecommons.org/publicdomain/zero/1.0/>

Filtered here to entries of 2–20 characters containing only Thai codepoints
(U+0E00–U+0E7F): phrases, abbreviations and latin-mixed entries are dropped.

## `words_en.txt.gz` — 315,100 English words

Source: [dwyl/english-words](https://github.com/dwyl/english-words) `words_alpha.txt`.

Licence: **The Unlicense** (public domain).
<https://unlicense.org/>

Filtered here to lower-case ASCII alphabetic entries of 2–12 characters.

## Regenerating

Both files are sorted, de-duplicated, newline-separated UTF-8, then gzipped.
They are embedded into `SmartTyping.Infrastructure` as resources.
