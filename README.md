# מערכת לניהול פניות (Requests Management)

מערכת לניהול פניות מארגונים: שליפה, חיפוש וסינון של 100,000 פניות, עדכון סטטוס (בודד ומרובה), היסטוריית שינויים ונתונים מסכמים, כשכמה משתמשים עובדים במקביל בלי לדרוס זה את זה.

---

## טכנולוגיות

| | |
|---|---|
| **Backend** | .NET 8, ASP.NET Core Web API, Entity Framework Core 8.0.33 |
| **מסד נתונים** | SQL Server LocalDB |
| **Frontend** | Angular 21 (Standalone Components, Signals, RxJS) |
| **בדיקות** | xUnit, Microsoft.AspNetCore.Mvc.Testing |

## מבנה הפתרון

```
backend/
├── RequestsManagement.Api      – Controllers, טיפול גלובלי בשגיאות, Program (DI)
├── RequestsManagement.BL       – לוגיקה עסקית: Services, DTOs, Validation, Cache
├── RequestsManagement.DAL      – גישה לנתונים: DbContext, Repository, Migrations, Seed
└── RequestsManagement.Tests    – Unit ו-Integration

frontend/src/app/
├── models       – טיפוסים (קובץ לכל מודל)
├── services     – RequestsApiService (קריאות HTTP), RequestsStateService (מצב המסך)
└── components   – טבלה, סינון, סיכומים, פרטי פנייה, עדכון מרובה
```

**זרימת תלויות:** `Api → BL → DAL`. רק ה-DAL ניגש למסד הנתונים, ה-BL ממיר Entities ל-DTOs, וה-Api לא מכיל לוגיקה.
ב-Angular הרכיבים רק מציגים ומדווחים על פעולות. כל המצב, צינור ה-RxJS והקריאות לשרת נמצאים ב-Services.

---

## הוראות הרצה

**דרישות:** .NET 8 SDK, SQL Server LocalDB (מותקן עם Visual Studio; בדיקה: `sqllocaldb info`), Node.js 22.

מריצים את השרת ואת הלקוח **במקביל**, בשני חלונות טרמינל, מתיקיית השורש של הריפו.

### 1. Backend
```bash
cd backend
dotnet run --project RequestsManagement.Api
```
- ה-API עולה על `https://localhost:7112`, ו-Swagger זמין ב-`https://localhost:7112/swagger`
- בהרצה הראשונה מסד הנתונים `RequestsManagementDb` נוצר אוטומטית ונזרעות **100,000 פניות** (כדקה, ההתקדמות מוצגת בחלון). בהרצות הבאות שלב זה מדולג
- **יצירת הנתונים מחדש:** `dotnet run --project RequestsManagement.Api -- --reseed`
- אם הדפדפן מתריע על אישור HTTPS: `dotnet dev-certs https --trust`
- אפשר גם לפתוח את `backend/RequestsManagement.sln` ב-Visual Studio ולהריץ (F5)
- **שרת SQL אחר:** לשנות את `ConnectionStrings:RequestsDb` בקובץ `backend/RequestsManagement.Api/appsettings.json`

### 2. Frontend
```bash
cd frontend
npm install
npm start
```
- האפליקציה עולה על `http://localhost:4200`
- הבקשות ל-`/api` מועברות לשרת דרך Proxy (`frontend/proxy.conf.json`), ולכן השרת צריך לרוץ על `https://localhost:7112`

### 3. בדיקות
```bash
cd backend
dotnet test
```
בדיקות ה-Integration רצות מול מסד נתונים נפרד ב-LocalDB (`RequestsManagementDb_Tests`), שנוצר מחדש בכל הרצה, כך שנתוני הפיתוח לא נפגעים.

---

## מסד הנתונים והאינדקסים

**טבלאות:** `Requests` (כל שדות הפנייה, כולל `RowVersion`), ו-`RequestStatusHistory` (שורת Audit לכל שינוי סטטוס).

| אינדקס | למה |
|---|---|
| `(Status, CreatedAt)` | סינון לפי סטטוס וממוין לפי תאריך, ספירה לפי סטטוס |
| `(Priority, CreatedAt)` | סינון לפי עדיפות וממוין לפי תאריך, ספירה לפי עדיפות |
| `CreatedAt` | מיון ברירת המחדל וטווח תאריכים |
| `OrganizationName`, `AssignedTo` | סינון לפי ארגון ולפי מטפל |
| `Title`, `UpdatedAt` | מיון לפי כותרת ולפי תאריך עדכון |
| `RequestStatusHistory (RequestId, ChangedAt)` | היסטוריה של פנייה |

כל הסינון, המיון והדפדוף רצים ב-SQL (`IQueryable` + `Skip/Take`), כך שרק העמוד המבוקש נטען לזיכרון.

## Concurrency

**Optimistic Concurrency** עם עמודת `rowversion`. הלקוח מקבל את ה-`RowVersion` בשליפה ומחזיר אותו בעדכון, ו-EF שולח `UPDATE ... WHERE RowVersion = @expected`. אם מישהו שינה בינתיים, אף שורה לא מתעדכנת והשרת מחזיר **409 Conflict**. האכיפה ברמת מסד הנתונים, ולכן היא תקפה גם לשני עדכונים באותו רגע בדיוק (יש לכך בדיקה אוטומטית).

העדכון ושורת ה-Audit נשמרים **באותה טרנזקציה**. בלקוח, במקרה של 409, מוצגת הודעה והפנייה נטענת מחדש מהשרת.

**מעברי סטטוס מותרים:** `New → InProgress`, `InProgress → Waiting / Completed`, `Waiting → InProgress`. `Completed` סופי.

## Bulk Update

`PATCH /api/requests/status`: עדכון של עד 100 פניות בבקשה אחת.

נבחרה גישת **Partial Success**: כל פנייה מטופלת עצמאית ובטרנזקציה משלה, והתשובה מפרטת לכל פנייה אם עודכנה, לא נמצאה, התנגשה או שהמעבר אסור. **נימוק:** כשל של פנייה אחת לא צריך לחסום עדכון של 99 תקינות.

## Cache

הנתונים המסכמים נשמרים ב-`IMemoryCache` ל-**5 דקות**.
- **Invalidation:** ה-Cache מתנקה אחרי כל שינוי סטטוס, בודד או מרובה
- **מניעת Cache Stampede:** כשה-Cache ריק ומגיעות הרבה בקשות יחד, רק אחת מחשבת וכל השאר מקבלות את התוצאה שלה
- **חישוב שחפף לעדכון לא נשמר**, כדי שמספרים ישנים לא יישארו ב-Cache
- **ריבוי מופעי שרת:** היינו עוברים ל-Redis (`IDistributedCache`), כדי שה-Cache וה-Invalidation יהיו משותפים לכל המופעים. ה-Cache מוגדר מאחורי ממשק, כך שהמעבר לא משנה את ה-Service

---

## שתי החלטות טכנולוגיות משמעותיות

1. **Optimistic Concurrency עם `rowversion`** (ולא נעילות): לא נועלים פנייה בזמן שהמשתמש מתלבט, והבדיקה מתבצעת באותה פקודת `UPDATE`, כך שאין חלון זמן להתנגשות.
2. **שלוש שכבות עם Repository** (ולא פרויקט אחד, וגם לא Clean Architecture מלאה): ההפרדה נאכפת ע"י הקומפיילר וה-BL לא תלוי ב-EF Core, בלי להוסיף מורכבות מיותרת למערכת עם שתי טבלאות.

## מגבלה ידועה ושיפור אפשרי

**מגבלה:** אין אימות משתמשים. שם המשתמש ל-Audit נשלח בכותרת `X-User-Name` מהלקוח. במערכת אמיתית הוא היה נלקח מהמשתמש המאומת (JWT).

**שיפור:** החיפוש החופשי (`LIKE '%term%'`) סורק את כל הטבלה. מעבר ל-**Full-Text Search** של SQL Server היה מאיץ אותו משמעותית.

## בדיקות אוטומטיות

**27 מקרי בדיקה** (xUnit, ב-17 מתודות בדיקה): מעברי סטטוס, Validation, התנהגות ה-Cache, ובדיקות Integration מול SQL Server אמיתי: שליפה עם סינון, Concurrency (כולל שני עדכונים במקביל), 404 ו-Bulk.

## שימוש בכלי AI

נעזרתי ב-Claude להבנת האפיון, להסבר מושגים וחלופות, ולכתיבת טיוטות קוד ותיעוד. כל קוד נבדק והובן, ואומת בהרצה, ב-Swagger ובבדיקות האוטומטיות.

**מה בדקתי או שיניתי בעצמי:**
- זיהיתי שבגרסה הראשונה ה-BL ניגש ישירות ל-DbContext, ודרשתי להעביר את כל הגישה לנתונים ל-Repository
- זיהיתי כפילות ברשימת שדות המיון, שאוחדה ל-Enum אחד
- דרשתי ב-Angular קובץ לכל מודל, והעברת כל הלוגיקה מהרכיבים ל-Services
- בבדיקות ידניות נמצאו ותוקנו: Seeding שנתקע ב-LocalDB, תשובת 409 שהוחזרה כ-500, תאריכים בלי UTC, ומיון לפי כותרת שנכשל ב-Timeout (נוספו אינדקסים)


---

## ביצועים: מדידה על 100,000 רשומות

נבחרו שתי פעולות API מרכזיות. כל אחת נמדדה ב-SSMS עם **Actual Execution Plan** ו-`SET STATISTICS IO, TIME`, פעם בלי האינדקס (נאלצת לסרוק את הטבלה, `WITH (INDEX(0))`) ופעם כפי שהיא נשלחת מה-API. השאילתות זהות לאלה ש-EF Core שולח.

### 1. רשימת פניות מסוננת: `GET /api/requests?status=New&page=1&pageSize=20`

| | בלי אינדקס | עם האינדקס `(Status, CreatedAt)` |
|---|---|---|
| Execution Plan | Clustered Index Scan + Sort | **Index Seek**, בלי Sort |
| שורות שנקראו | 100,000 (נמצאו 25,028 חדשות, וכולן מוינו) | 44 |
| Logical reads | 2,084 | **167** |
| זמן | 483 ms | ~0 ms (125 ms בהרצה הראשונה, כשהאינדקס נקרא מהדיסק) |

האינדקס כבר ממוין לפי סטטוס ובתוכו לפי תאריך, ולכן SQL Server קופץ לאזור של הסטטוס, קורא 20 שורות לפי הסדר ועוצר. את שאר העמודות הוא משלים (Key Lookup) רק עבור 20 השורות של העמוד.

### 2. נתונים מסכמים: `GET /api/requests/statistics`

| | בלי אינדקס | עם האינדקסים `(Status, CreatedAt)` / `(Priority, CreatedAt)` |
|---|---|---|
| Execution Plan | Clustered Index Scan + Hash Match | **Index Scan** על האינדקס הצר + Stream Aggregate |
| Logical reads (לפי סטטוס) | 2,084 | **626** |
| זמן | 158 ms | 0 ms |

ספירה תמיד עוברת על כל השורות, אבל מאינדקס צר שתופס הרבה פחות דפים מהטבלה המלאה, וכבר ממוין לפי הסטטוס. בנוסף, התוצאה נשמרת ב-Cache, כך שרוב הקריאות לא מגיעות למסד הנתונים בכלל.

### Bottleneck שזוהה ותוקן: מיון לפי כותרת

בבדיקה ידנית, מיון לפי "כותרת" נכשל ב-Timeout אחרי 30 שניות. לא היה אינדקס על `Title`, ולכן SQL Server היה צריך למיין 100,000 שורות לפי טקסט, פעולה שדורשת זיכרון ש-LocalDB לא קיבל. **התיקון:** נוספו אינדקסים על `Title` ועל `UpdatedAt`, וסדר המיון ב-Repository יושר לאינדקסים (כל מיון הוא "שדה, ואחריו Id", ובסטטוס ובעדיפות "שדה, CreatedAt, Id"). עכשיו כל מיון קורא אינדקס לפי הסדר, בלי פעולת Sort.

### Bottleneck ידוע והצעת שיפור

החיפוש החופשי (`LIKE '%term%'`) לא יכול להשתמש באינדקס וסורק את כל הטבלה. ב-100,000 שורות זה עדיין מהיר מספיק, וה-Debounce בלקוח מצמצם את מספר הבקשות. **הצעת שיפור:** Full-Text Search של SQL Server על `Title` ו-`OrganizationName`.


---

## תכנון עבודה

### החלטות שהתקבלו לפני כתיבת הקוד

| נושא | החלטה | למה |
|---|---|---|
| מסד נתונים | SQL Server + EF Core 8, Code-First | `rowversion` מובנה, Execution Plans, הסכמה נוצרת אוטומטית |
| מבנה | 3 שכבות: Api / BL / DAL + Tests | הפרדת אחריות שנאכפת ע"י הקומפיילר |
| Concurrency | `RowVersion` נשלח מהלקוח ונאכף במסד הנתונים | מונע Lost Update, לא רק ב-UI |
| מעברי סטטוס | `New → InProgress → Waiting / Completed`, `Waiting → InProgress` | כללים ברורים וניתנים לבדיקה |
| Bulk | Partial Success | כשל של פנייה אחת לא חוסם את השאר |
| Cache | הנתונים המסכמים + Invalidation בכל עדכון | חישוב יקר שנקרא הרבה |
| `ChangedBy` | כותרת `X-User-Name` | אין אימות במבדק, מתועד כמגבלה |

### משימות (לפי סדר ביצוע)

| # | משימה | תוצר | תלוי ב- | הערכה | סטטוס |
|---|---|---|---|---|---|
| 1 | ניתוח האפיון והחלטות | טבלת ההחלטות | – | 1h | ✅ |
| 2 | ריפו ו-Solution עם 4 פרויקטים | Solution שנבנה ב-GitHub | 1 | 1h | ✅ |
| 3 | תוכנית עבודה | מסמך זה | 1 | 0.5h | ✅ |
| 4 | ישויות, DbContext, הגדרות ו-`rowversion` | מודל הנתונים | 2 | 1h | ✅ |
| 5 | אינדקסים ו-Migration | מסד נתונים ב-LocalDB | 4 | 0.5h | ✅ |
| 6 | זריעת 100,000 פניות + `--reseed` | Seeder | 5 | 1h | ✅ |
| 7 | DTOs ו-Validation | טפסים ותשובות ב-BL | 4 | 1h | ✅ |
| 8 | Repository ו-Service לשליפה: סינון, חיפוש, מיון, דפדוף | שליפה בצד השרת | 5, 7 | 1.5h | ✅ |
| 9 | Aggregations ו-Controller | 3 כתובות GET + Swagger | 8 | 0.5h | ✅ |
| 10 | טיפול גלובלי בשגיאות, Logging, DI | Problem Details | 2 | 0.5h | ✅ |
| 11 | עדכון סטטוס, Concurrency ומעברים (404/400/409) | `PATCH .../{id}/status` | 9, 10 | 1h | ✅ |
| 12 | Audit והיסטוריה | `GET .../{id}/history` | 11 | 0.5h | ✅ |
| 13 | Bulk (Partial Success) | `PATCH .../status` | 12 | 1h | ✅ |
| 14 | Cache, Invalidation והגנה מ-Stampede | Cache לנתונים המסכמים | 9, 13 | 1h | ✅ |
| 15 | בדיקות Unit ו-Integration | 27 מקרי בדיקה | 13, 14 | 2h | ✅ |
| 16 | Angular: הקמה, Proxy, מודלים ו-Services | אפליקציה רצה | 9 | 0.5h | ✅ |
| 17 | Angular: טבלה, דפדוף ומיון, מצבי Loading/Empty/Error | רשימת פניות | 16 | 1.5h | ✅ |
| 18 | Angular: חיפוש וסינון עם Debounce ו-`switchMap` | סינון | 17 | 0.5h | ✅ |
| 19 | Angular: פרטי פנייה, עדכון סטטוס, 409 והיסטוריה | חלון פרטים | 17, 11 | 1h | ✅ |
| 20 | Angular: נתונים מסכמים ועדכון מרובה | סיכומים + סרגל Bulk | 17, 13 | 1h | ✅ |
| 21 | מדידת ביצועים | סעיף הביצועים | 6, 9 | 1h | ✅ |
| 22 | README ובדיקת הרצה מאפס | הגשה | הכול | 1.5h | ✅ |

**סה"כ הערכה:** כ-21 שעות. בפועל העבודה ארכה יותר, בעיקר בגלל לימוד כל חלק לעומק והתקלות שבטבלה למטה.

**תלויות עיקריות:** מודל הנתונים חוסם את כל השאר · השליפה היא הבסיס לעדכון, שהוא הבסיס ל-Bulk · ה-Cache תלוי בכל פעולות העדכון (Invalidation) · Angular ובדיקות ה-Integration תלויים ב-API מוכן.

**משימות שאינן קוד:** תכנון המודל והאינדקסים, מדידת ביצועים, תיעוד, בדיקת הרצה.

### מה השתנה לעומת התכנון

| שינוי | למה |
|---|---|
| נוספה שכבת **Repository** ב-DAL | בגרסה הראשונה ה-BL ניגש ישירות ל-DbContext, בניגוד להפרדת השכבות |
| ה-Seeder עבר מ-`SqlBulkCopy` להכנסה במנות של 5,000 | `SqlBulkCopy` המתין ללא הגבלה להקצאת זיכרון ב-LocalDB (`RESOURCE_SEMAPHORE`) |
| נוסף ממיר UTC לתאריכים | תאריכים חזרו ממסד הנתונים בלי סימון UTC, והלקוח היה מציג שעה שגויה |
| תיקון בטיפול בשגיאות | כשהלקוח ביקש `text/plain`, תשובת 409 הפכה ל-500. עכשיו תמיד נכתב `application/problem+json` |
| אינדקסים על `Title` ו-`UpdatedAt` | מיון לפי כותרת נכשל ב-Timeout (ראו ביצועים) |
| עדכון מרובה מהממשק והגנה מ-Cache Stampede | ה-Bulk היה זמין רק ב-API, וה-Cache היה חשוף לחישובים מקבילים מיד אחרי Invalidation |