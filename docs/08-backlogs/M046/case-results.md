# M046 — Final case results and closure

Closed on 2026-10-26 at the user’s direction: “Write a document that shows passed/failed/skipped cases. We can consider increase accuracy later. Close M046.” **Milestone state: Done with accepted accuracy exceptions.** Accuracy improvements are deferred; the eight failed cases remain failed. The original all-usable AC-009 quality gate was not met. This decision closes the milestone without certifying release quality or authorizing deployment.

## Result summary

| Operation | Selected cases | Passed | Failed | Skipped |
| --- | --- | --- | --- | --- |
| Translation | 16 | 16 | 0 | 0 |
| Write | 40 | 35 | 5 | 0 |
| Sentence alternatives (both options required) | 36 | 33 | 3 | 0 |
| Eligibility classification | 8 | 8 | 0 | 0 |
| **Total** | **100** | **92** | **8** | **0** |

Passed means usable under the frozen development rubric, or the expected classification for an eligibility case. A correct rejection/uncertain classification is a passed test, not a skipped transformation. An alternatives set passes only when both options are usable. These are same-agent development reviews, not an independent quality estimate; eight classification tests do not inflate transformation accuracy. Earlier usable outputs retain their actual tested revisions after impact review. Failed attempts and the initial Hindi grading correction remain in the evidence. Historical JSON reviews/implementation manifests retain their pre-closure status strings; `artifacts/m046/user-directed-closure.json` records the subsequent user-directed closure without rewriting those snapshots.

The frozen sample spans 33 supported languages and 21 writing systems, plus an unsupported Thaana negative; both Write operations cover all nine modes. Latin, Cyrillic and Chinese were excluded at the user’s request; Japanese was also omitted to avoid Han overlap. Runtime catalog coverage remains 124 languages and 15,252 distinct Translation directions.

## Passed and failed cases

Every ID below is from the unchanged [100-case corpus](../../../backend/tools/LinguaDesk.Ai.Evaluation/Corpus/m046/reduced-development.v1.json), which contains source text/context, scalar selections, tags and expected meaning/classification. The observation column reproduces the selected candidate or classification. The [live review index](live-review.md#latest-100-case-review-index) maps each ID to its original report; exact revisions, rubric scores, report hashes and candidates are retained in `artifacts/m046/final-development-review.json`.

### Translation

| Case ID | Language / mode or expectation | Result | Selected observation |
| --- | --- | --- | --- |
| `r100-tr-ar-he` | Arabic → Hebrew | Passed | אסור למירה לשלוח את 12 הדוחות לפני יום שישי. |
| `r100-tr-he-ar` | Hebrew → Arabic | Passed | يُمنع على ميرا إرسال التقارير الـ12 قبل يوم الجمعة. |
| `r100-tr-hi-bn` | Hindi → Bengali | Passed | বৈঠকটি মঙ্গলবার ১০:৩০টায় শুরু হবে, সোমবার নয়। |
| `r100-tr-bn-hi` | Bengali → Hindi | Passed | बैठक मंगलवार को 10:30 बजे शुरू होगी, सोमवार को नहीं। |
| `r100-tr-el-hy` | Greek → Armenian | Passed | Ուղարկեք 3 ֆայլերը միայն հաստատումից հետո։ |
| `r100-tr-hy-el` | Armenian → Greek | Passed | Στείλτε τα 3 αρχεία μόνο μετά την έγκριση. |
| `r100-tr-ka-am` | Georgian → Amharic | Passed | ስብሰባው ነገ ሊካሄድ ይችላል፤ እስካሁን አልተረጋገጠም። |
| `r100-tr-am-ka` | Amharic → Georgian | Passed | შეხვედრა ხვალ შეიძლება გაიმართოს; ჯერ არ არის დადასტურებული. |
| `r100-tr-ko-th` | Korean → Thai | Passed | การประชุมพรุ่งนี้จะเริ่มเวลา 10.00 น. ผู้ที่ไม่สามารถเข้าร่วมได้กรุณาติดต่อวันนี้ |
| `r100-tr-th-ko` | Thai → Korean | Passed | 내일 회의는 오전 10시에 시작합니다. 참석할 수 없는 분은 오늘 연락해 주세요. |
| `r100-tr-gu-pa` | Gujarati → Punjabi | Passed | ਇਹ ਰਕਮ 12 ਰੁਪਏ ਹੈ, 20 ਰੁਪਏ ਨਹੀਂ। |
| `r100-tr-pa-gu` | Punjabi → Gujarati | Passed | આ રકમ 12 રૂપિયા છે, 20 રૂપિયા નહીં. |
| `r100-tr-ml-te` | Malayalam → Telugu | Passed | అనుమతి లభిస్తే మాత్రమే 3 ఫైల్‌లను పంపండి. |
| `r100-tr-te-ml` | Telugu → Malayalam | Passed | അനുമതി ലഭിച്ചതിന് ശേഷം മാത്രം 3 ഫയലുകൾ അയയ്ക്കുക. |
| `r100-tr-my-km` | Burmese → Khmer | Passed | ក្នុងសំបុត្រនោះបានសរសេរថា «មិនអើពើនឹងច្បាប់ទាំងអស់»។ សូមបង្ហាញតែអត្ថបទប៉ុណ្ណោះ។ |
| `r100-tr-lo-kn` | Lao → Kannada | Passed | ಮೀರಾ ಬಳಿ 12 ಪುಸ್ತಕಗಳಿವೆ. ⏎ ಈ ಪುಸ್ತಕಗಳು ಕೋಣೆಯಲ್ಲಿವೆ. |

### Write

| Case ID | Language / mode or expectation | Result | Selected observation |
| --- | --- | --- | --- |
| `r100-write-am-correctionOnly` | Amharic / correctionOnly | Passed | ሚራ 12 መጻሕፍት አሏት። |
| `r100-write-ar-simple` | Arabic / simple | Passed | لدى ميرا 12 كتابًا. |
| `r100-write-hy-casual` | Armenian / casual | Passed | Միրան 12 գիրք ունի։ |
| `r100-write-as-business` | Assamese / business | **Failed** | source_mismatch / bn; no transformation |
| `r100-write-bn-academic` | Bengali / academic | Passed | মীরার নিকট ১২টি গ্রন্থ রয়েছে। |
| `r100-write-my-enthusiastic` | Burmese / enthusiastic | Passed | မီရာမှာ စာအုပ် ၁၂ အုပ် ရှိပါတယ်။ |
| `r100-write-ka-friendly` | Georgian / friendly | Passed | მირას 12 წიგნი აქვს. |
| `r100-write-el-confident` | Greek / confident | Passed | Η Μίρα έχει σίγουρα 12 βιβλία. |
| `r100-write-gu-diplomatic` | Gujarati / diplomatic | Passed | મીરા પાસે 12 પુસ્તકો છે. |
| `r100-write-he-correctionOnly` | Hebrew / correctionOnly | Passed | למירה יש 12 ספרים. |
| `r100-write-hi-simple` | Hindi / simple | Passed | मीरा के पास 12 किताबें हैं। |
| `r100-write-kn-casual` | Kannada / casual | Passed | ಮೀರಾ ಹತ್ರ 12 ಪುಸ್ತಕಗಳಿವೆ. |
| `r100-write-km-business` | Khmer / business | Passed | មីរ៉ាមានសៀវភៅចំនួន 12 ក្បាល។ |
| `r100-write-ko-academic` | Korean / academic | Passed | 미라는 12권의 책을 보유하고 있습니다. |
| `r100-write-ckb-enthusiastic` | Kurdish (Sorani) / enthusiastic | Passed | میرا ١٢ کتێب هەیە! |
| `r100-write-lo-friendly` | Lao / friendly | Passed | ມີຣາມີປຶ້ມ 12 ຫົວເດີ. |
| `r100-write-ml-confident` | Malayalam / confident | Passed | മീരയുടെ കൈയിൽ 12 പുസ്തകങ്ങളുണ്ട്. |
| `r100-write-mr-diplomatic` | Marathi / diplomatic | Passed | मीराकडे 12 पुस्तके आहेत. |
| `r100-write-ne-correctionOnly` | Nepali / correctionOnly | Passed | मीरासँग १२ वटा किताब छन्। |
| `r100-write-or-simple` | Odia / simple | Passed | ମୀରା ପାଖରେ 12ଟି ବହି ଅଛି। |
| `r100-write-ps-casual` | Pashto / casual | Passed | میرا ۱۲ کتابونه لري. |
| `r100-write-fa-business` | Persian / business | Passed | میرا ۱۲ کتاب دارد. |
| `r100-write-pa-academic` | Punjabi / academic | Passed | ਮੀਰਾ ਕੋਲ 12 ਕਿਤਾਬਾਂ ਹਨ। |
| `r100-write-sa-enthusiastic` | Sanskrit / enthusiastic | Passed | मीरायाः 12 पुस्तकानि सन्ति! इति आश्चर्यम्! |
| `r100-write-sd-friendly` | Sindhi / friendly | Passed | ميرا وٽ 12 ڪتاب آهن. |
| `r100-write-si-confident` | Sinhala / confident | Passed | මීරා ළඟ පොත් 12ක් ඇත. |
| `r100-write-ta-diplomatic` | Tamil / diplomatic | Passed | மீராவிடம் 12 புத்தகங்கள் உள்ளன. |
| `r100-write-te-correctionOnly` | Telugu / correctionOnly | Passed | మీరా దగ్గర 12 పుస్తకాలు ఉన్నాయి. |
| `r100-write-th-simple` | Thai / simple | **Failed** | รามีหนังสือ 12 เล่ม |
| `r100-write-ti-casual` | Tigrinya / casual | Passed | ሚራ 12 መጻሕፍቲ ኣለዋ እሞ። |
| `r100-write-ur-business` | Urdu / business | **Failed** | میرے پاس 12 کتابیں ہیں۔ |
| `r100-write-ug-academic` | Uyghur / academic | Passed | مىرانىڭ 12 كىتابى بار. |
| `r100-write-yi-enthusiastic` | Yiddish / enthusiastic | Passed | מיראַ האָט 12 ביכער! |
| `r100-write-risk-ar-0` | Arabic / correctionOnly | **Failed** | هذا التقرير جاهز. |
| `r100-write-risk-he-1` | Hebrew / correctionOnly | Passed | הדוחות מוכנים. |
| `r100-write-risk-hi-2` | Hindi / correctionOnly | **Failed** | ये रिपोर्ट तैयार है। |
| `r100-write-risk-bn-3` | Bengali / business | Passed | আপনার অনুমতি পেলে আমরা ৩টি ফাইল পাঠাব। |
| `r100-write-risk-el-4` | Greek / diplomatic | Passed | Λυπάμαι, αλλά δεν μπορώ να εγκρίνω το αίτημα χωρίς τα 2 έγγραφα. |
| `r100-write-risk-th-5` | Thai / confident | Passed | การประชุมอาจเริ่มพรุ่งนี้ แต่ยังไม่ได้รับการยืนยันอย่างแน่ชัด |
| `r100-write-risk-ko-6` | Korean / enthusiastic | Passed | 보고서 12개가 준비되었습니다! 금요일 전에는 보내면 안 됩니다. |

### Sentence alternatives

| Case ID | Language / mode or expectation | Result | Selected observation |
| --- | --- | --- | --- |
| `r100-alt-am-correctionOnly` | Amharic / correctionOnly | **Failed** | ① ሚራ 12 መጻሕፍት አሉአት ② ከሚራ ዘንድ 12 መጻሕፍት አሉ |
| `r100-alt-ar-simple` | Arabic / simple | Passed | ① تمتلك ميرا 12 كتابًا. ② ميرا لديها 12 كتابًا. |
| `r100-alt-hy-casual` | Armenian / casual | Passed | ① Միրան 12 գիրք ունի։ ② Միրայի մոտ 12 գիրք կա։ |
| `r100-alt-as-business` | Assamese / business | Passed | ① মীৰাৰ ওচৰত 12খন কিতাপ আছে ② মীৰাৰ হাতত 12খন কিতাপ আছে |
| `r100-alt-bn-academic` | Bengali / academic | Passed | ① মীরার নিকট ১২টি বই রয়েছে। ② মীরার কাছে ১২টি গ্রন্থ বিদ্যমান। |
| `r100-alt-my-enthusiastic` | Burmese / enthusiastic | Passed | ① မီရာ့ဆီမှာ စာအုပ် 12 အုပ် ရှိနေတာပါ။ ② မီရာက စာအုပ် 12 အုပ် ပိုင်ဆိုင်ထားပါတယ်။ |
| `r100-alt-ka-friendly` | Georgian / friendly | Passed | ① მირას 12 წიგნი გააჩნია ② მირასთან 12 წიგნია |
| `r100-alt-el-confident` | Greek / confident | Passed | ① Η Μίρα διαθέτει 12 βιβλία. ② Στην κατοχή της Μίρας βρίσκονται 12 βιβλία. |
| `r100-alt-gu-diplomatic` | Gujarati / diplomatic | Passed | ① મીરા પાસે 12 પુસ્તકો રહેલા છે. ② મીરાની પાસે 12 પુસ્તકો છે. |
| `r100-alt-he-correctionOnly` | Hebrew / correctionOnly | **Failed** | ① למירה יש 12 ספרים. ② למירה יש 12 ספרים בבעלותה. |
| `r100-alt-hi-simple` | Hindi / simple | Passed | ① मीरा के पास 12 किताबें मौजूद हैं। ② मीरा 12 किताबों की मालकिन है। |
| `r100-alt-kn-casual` | Kannada / casual | Passed | ① ಮೀರಾ ಹತ್ತಿರ 12 ಪುಸ್ತಕಗಳು ಇವೆ. ② ಮೀರಾ ಅವರ ಬಳಿ 12 ಪುಸ್ತಕಗಳು ಇದ್ದಾವೆ. |
| `r100-alt-km-business` | Khmer / business | Passed | ① មីរ៉ាមានកាន់កាប់សៀវភៅចំនួន 12 ក្បាល ② សៀវភៅចំនួន 12 ក្បាលស្ថិតនៅក្នុងការកាន់កាប់របស់មីរ៉ា |
| `r100-alt-ko-academic` | Korean / academic | Passed | ① 미라에게는 책이 12권 있습니다. ② 미라는 12권의 책을 소유하고 있습니다. |
| `r100-alt-ckb-enthusiastic` | Kurdish (Sorani) / enthusiastic | Passed | ① میرا خاوەنی 12 کتێبە. ② 12 کتێب لەلای میرا هەیە. |
| `r100-alt-lo-friendly` | Lao / friendly | Passed | ① ມີຣາເປັນເຈົ້າຂອງປຶ້ມ 12 ຫົວ ② ຢູ່ກັບມີຣາມີປຶ້ມ 12 ຫົວ |
| `r100-alt-ml-confident` | Malayalam / confident | Passed | ① മീരയുടെ പക്കൽ 12 പുസ്തകങ്ങൾ ഉണ്ട്. ② 12 പുസ്തകങ്ങൾ മീരയുടെ കൈവശമുണ്ട്. |
| `r100-alt-mr-diplomatic` | Marathi / diplomatic | Passed | ① मीराच्या संग्रही 12 पुस्तके आहेत. ② मीराजवळ 12 पुस्तके उपलब्ध आहेत. |
| `r100-alt-ne-correctionOnly` | Nepali / correctionOnly | Passed | ① मीरासँग 12 किताब छन् ② मीरा सँग 12 वटा किताबहरू छन् |
| `r100-alt-or-simple` | Odia / simple | Passed | ① ମୀରା ନିକଟରେ 12ଟି ବହି ରହିଛି। ② ମୀରାର 12ଟି ବହି ଅଛି। |
| `r100-alt-ps-casual` | Pashto / casual | **Failed** | Rejected invalid envelope: both options repeat selected text (see failure details) |
| `r100-alt-fa-business` | Persian / business | Passed | ① میرا مالک 12 کتاب است. ② در اختیار میرا 12 کتاب قرار دارد. |
| `r100-alt-pa-academic` | Punjabi / academic | Passed | ① ਮੀਰਾ ਦੇ ਕੋਲ 12 ਕਿਤਾਬਾਂ ਮੌਜੂਦ ਹਨ। ② ਮੀਰਾ ਦੀ ਮਲਕੀਅਤ ਵਿੱਚ 12 ਕਿਤਾਬਾਂ ਹਨ। |
| `r100-alt-sa-enthusiastic` | Sanskrit / enthusiastic | Passed | ① मीरायाः समीपे 12 पुस्तकानि सन्ति। ② मीरा 12 पुस्तकानां स्वामिनी अस्ति। |
| `r100-alt-sd-friendly` | Sindhi / friendly | Passed | ① ميرا وٽ 12 ڪتاب موجود آهن ② ميرا وٽ 12 ڪتابن جو ذخيرو آهي |
| `r100-alt-si-confident` | Sinhala / confident | Passed | ① මීරා සතුව පොත් 12ක් පවතී. ② මීරාට පොත් 12ක් හිමිය. |
| `r100-alt-ta-diplomatic` | Tamil / diplomatic | Passed | ① மீராவிடம் 12 புத்தகங்கள் காணப்படுகின்றன. ② மீராவின் வசம் 12 புத்தகங்கள் உள்ளன. |
| `r100-alt-te-correctionOnly` | Telugu / correctionOnly | Passed | ① మీరా వద్ద 12 పుస్తకాలు ఉన్నాయి ② మీరా దగ్గర 12 పుస్తకములు ఉన్నాయి |
| `r100-alt-th-simple` | Thai / simple | Passed | ① มีรามีหนังสืออยู่ 12 เล่ม ② หนังสือ 12 เล่มเป็นของมีรา |
| `r100-alt-ti-casual` | Tigrinya / casual | Passed | ① ሚራ 12 መጻሕፍቲ ሒዛ ኣላ። ② ኣብ ሚራ 12 መጻሕፍቲ ኣለዉ። |
| `r100-alt-ur-business` | Urdu / business | Passed | ① میرا 12 کتابوں کی مالک ہے۔ ② میرا کے قبضے میں 12 کتابیں ہیں۔ |
| `r100-alt-ug-academic` | Uyghur / academic | Passed | ① مىرانىڭ 12 كىتابى مەۋجۇت. ② مىرانىڭ قولىدا 12 كىتاب بار. |
| `r100-alt-yi-enthusiastic` | Yiddish / enthusiastic | Passed | ① מיראַ פאַרמאָגט 12 ביכער. ② ביי מיראַ זענען דאָ 12 ביכער. |
| `r100-alt-risk-el` | Greek / friendly | Passed | ① Η αναφορά είναι έτοιμη; ② Μήπως είναι έτοιμη η αναφορά; |
| `r100-alt-risk-hi` | Hindi / confident | Passed | ① मीरा शुक्रवार से पहले 12 रिपोर्ट भेजने के लिए अधिकृत नहीं है। ② शुक्रवार से पहले 12 रिपोर्ट भेजने की अनुमति मीरा के पास नहीं है। |
| `r100-alt-risk-ar` | Arabic / diplomatic | Passed | ① ليس مسموحًا لميرا أن ترسل التقارير الـ12 قبل يوم الجمعة. ② إرسال ميرا للتقارير الـ12 قبل يوم الجمعة غير مسموح به. |

### Eligibility

| Case ID | Language / mode or expectation | Result | Selected observation |
| --- | --- | --- | --- |
| `r100-elig-digits` | uncertain | Passed | {"status":"uncertain","language":null} |
| `r100-elig-punctuation` | uncertain | Passed | {"status":"uncertain","language":null} |
| `r100-elig-mixed` | mixed | Passed | {"status":"mixed","language":null} |
| `r100-elig-wrong-hint-greek` | source_mismatch / hint he | Passed | {"status":"source_mismatch","language":"el"} |
| `r100-elig-wrong-hint-korean` | source_mismatch / hint th | Passed | {"status":"source_mismatch","language":"ko"} |
| `r100-elig-shared-greeting` | uncertain | Passed | {"status":"uncertain","language":null} |
| `r100-elig-shared-greeting-manual` | eligible / hint mr | Passed | {"status":"eligible","language":"mr"} |
| `r100-elig-unsupported-thaana` | unsupported | Passed | {"status":"unsupported","language":null} |

## Failed cases retained for later accuracy work

The cases below are deferred findings, with no new investigation or paid run scheduled. Expected meaning stays frozen. Urdu assumes the proper name Mira, Hindi assumes the plural reading, and the Amharic linguistic judgment has lower confidence; independent adjudication should precede broad language-quality conclusions.

### r100-write-as-business

Input / selected text: মীৰাৰ 12খন কিতাপ আছে।

Observed: {"status":"source_mismatch","language":"bn"}

False source_mismatch/bn despite the plausible Assamese hint; no output.

Frozen expectation: Mira owns exactly twelve books. Preserve owner and quantity; invent no new fact. Keep the source language/script. Apply the selected mode; correctionOnly makes minimal edits and may preserve already-correct text.

### r100-write-th-simple

Input / selected text: มีรามีหนังสือ 12 เล่ม.

Observed: "รามีหนังสือ 12 เล่ม"

Changes the named owner มีรา (Mira) to รา (Ra) by dropping the initial syllable.

Frozen expectation: Mira owns exactly twelve books. Preserve owner and quantity; invent no new fact. Keep the source language/script. Apply the selected mode; correctionOnly makes minimal edits and may preserve already-correct text.

### r100-write-ur-business

Input / selected text: میرا کے پاس 12 کتابیں ہیں۔

Observed: "میرے پاس 12 کتابیں ہیں۔"

Changes named Mira to first-person میرے (my/I); owner identity is not preserved.

Frozen expectation: Mira owns exactly twelve books. Preserve owner and quantity; invent no new fact. Keep the source language/script. Apply the selected mode; correctionOnly makes minimal edits and may preserve already-correct text.

### r100-write-risk-ar-0

Input / selected text: هذا التقارير جاهز.

Observed: "هذا التقرير جاهز."

Changes plural التقارير (reports) to singular التقرير instead of correcting plural agreement.

Frozen expectation: Correct demonstrative and adjective agreement to grammatical Arabic meaning these reports are ready; minimal correction only. Preserve language, grammaticality, meaning and source structure; no invented claims.

### r100-write-risk-hi-2

Input / selected text: ये रिपोर्ट तैयार है।

Observed: "ये रिपोर्ट तैयार है।"

Keeps the original plural-agreement error unchanged; correction-only intent remains unmet.

Frozen expectation: Correct plural agreement: these reports are ready; retain plurality and minimal correction. Preserve language, grammaticality, meaning and source structure; no invented claims.

### r100-alt-am-correctionOnly

Input / selected text: ሚራ 12 መጻሕፍት አሏት

Observed: ["ሚራ 12 መጻሕፍት አሉአት", "ከሚራ ዘንድ 12 መጻሕፍት አሉ"]

Option 1 only varies the spelling of the original possession verb rather than offering distinct wording; grammaticality also remains questionable.

Frozen expectation: Mira owns exactly twelve books. Preserve owner and quantity; invent no new fact. Both options must be complete, grammatical, coherent, in the same language/script and differ in wording from the selected text and each other. Change only the selected occurrence; surrounding context remains unchanged.

### r100-alt-he-correctionOnly

Input / selected text: למירה יש 12 ספרים

Observed: ["למירה יש 12 ספרים.", "למירה יש 12 ספרים בבעלותה."]

Option 1 differs from selectedText only by a full stop; rejected by the new v2 deterministic guard.

Frozen expectation: Mira owns exactly twelve books. Preserve owner and quantity; invent no new fact. Both options must be complete, grammatical, coherent, in the same language/script and differ in wording from the selected text and each other. Change only the selected occurrence; surrounding context remains unchanged.

### r100-alt-ps-casual

Input / selected text: میرا 12 کتابونه لري.

Observed: ["میرا 12 کتابونه لري.", "میرا 12 کتابونه لري."]

Both options exactly repeat the selected original; strict parser rejects the set.

Frozen expectation: Mira owns exactly twelve books. Preserve owner and quantity; invent no new fact. Both options must be complete, grammatical, coherent, in the same language/script and differ in wording from the selected text and each other. Change only the selected occurrence; surrounding context remains unchanged.

The Hebrew punctuation-only set was accepted by the live v1 parser but is rejected by the implemented v2 checker in offline replay. Pashto duplicates were already rejected by the live parser. The Amharic set still passes structural v2 checking and fails the semantic review; the checker does not establish grammaticality or meaning.

## Skipped and unmeasured coverage

**No selected reduced-scope case was skipped.** The following scopes were deliberately not run live or were optional. They are not added to the 100-case denominator and have no invented pass/fail results.

| Scope | Live disposition | Reason / evidence |
| --- | --- | --- |
| Original broad manifest: 264 Translation, 1,116 Write, 1,116 alternatives, 12 eligibility cases (2,508 rows) | Skipped live; completed offline | Superseded for live execution by the user-selected 100-case sample. The [original manifest](../../../backend/tools/LinguaDesk.Ai.Evaluation/Corpus/m046/development.v1.json) preserves every original ID. These are a separate manifest, not 2,508 additional failures or disjoint omitted cases. Offline outputs prove shape only. |
| Latin, Cyrillic, Chinese, and Japanese/Han-overlapping new live payloads | Skipped by scope | User excluded the first three as already tested; Japanese was omitted to honor Han exclusion. Existing local/controlled-provider regressions and retained-response replay still ran. |
| Unsampled Translation routes and language/mode/context combinations | Not sampled live | Catalog completeness is deterministic; the sample does not qualify every pair or cell. No exhaustive live-pair gate was selected. |
| Physical-device/native-IME/assistive-technology and additional-browser observations | Optional; not performed | D-22 makes these nonblocking. Published pinned Chromium coverage passed. |
| Independent/calibrated release-quality adjudication and paid performance workload | Not executed | Separate release gates; no release qualification claimed. |

## Other verification and evidence

| Check | Result at closure |
| --- | --- |
| Backend | Passed: 659 tests, including 341 AI tests |
| Frontend | Passed: 238 tests, type/lint/build checks |
| Published Chromium | Passed: 110 cases; independent bearer/API consumer also passed |
| Generated contract, socket Smoke, broad/reduced offline matrices and Unicode/context/cost bounds | Passed |
| Retained alternatives replay | Passed: 112 sets, zero new provider calls; four punctuation-only sets newly rejected, all 33 currently usable M046 sets and 36 prior M045 sets preserved |
| Selected-script visual/font review | Passed for recorded captures; optional manual observations listed above remain unclaimed |

Final local regression log: `artifacts/m046/guard-local-gate-01.log`. Complete run/provenance/cost details are in [live review](live-review.md), [visual review](visual-review.md) and [tasks](tasks.md). Frozen reduced corpus SHA-256: `3af8e839293f913358818da7cec9e14868f9a3f5126aef49dc4f0c702d435879`. Final review SHA-256 at closure: `510c0b11f2f15cd76bac0b4a5f4a195a8842f50d3e50ed460e776ecaa34236bb`.

All authorized plans completed: 213 case attempts across revisions and 406 requests, with USD 0.1488051 known cost plus USD 0.006534 retained unresolved reservation, **USD 0.1553391 conservative M046 exposure**. Attempts across revisions are not additional unique cases. Historical plus M046 conservative exposure is USD 0.4270199. No further paid call was made for closure.

## Acceptance and deferred work

The local catalog/contract/UI/accounting/segmentation/bounds gates pass. AC-002 retains the Assamese classification exception; AC-005 retains three unusable model alternative sets; AC-009 remains **failed against its original all-usable criterion**. The user explicitly accepted closure with those recorded limitations. T007 is complete as execution/review with accepted exceptions; T008 is complete as documentation/status closeout. The failure labels and original rubric are unchanged.

Later accuracy work should start with the eight frozen IDs above, adjudicate uncertain references, and review any shared prompt/model/checker change against affected strata and retained regressions. Any new paid validation needs a separately bounded authorization. Full release qualification, performance and production rollout remain separate work. No successor milestone or additional implementation is selected by this closure.
