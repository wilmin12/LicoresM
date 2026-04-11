-- ============================================================
-- 39 – Alter FREIGHT_FORWARDERS column sizes + Seed data
--      Source: FREIGHT_FORWARDERS.xlsx
-- ============================================================

SET NOCOUNT ON;

-- ── 1. Ampliar columnas que se quedaban cortas ─────────────
ALTER TABLE FREIGHT_FORWARDERS ALTER COLUMN FF_NAME      NVARCHAR(100) NOT NULL;
ALTER TABLE FREIGHT_FORWARDERS ALTER COLUMN FF_COUNTRY   NVARCHAR(50)  NULL;
ALTER TABLE FREIGHT_FORWARDERS ALTER COLUMN FF_PHONE_1   NVARCHAR(30)  NULL;
ALTER TABLE FREIGHT_FORWARDERS ALTER COLUMN FF_PHONE_2   NVARCHAR(30)  NULL;
ALTER TABLE FREIGHT_FORWARDERS ALTER COLUMN FF_CONTACT   NVARCHAR(100) NULL;
PRINT 'Column sizes updated.';
GO

-- ── 2. Insertar / actualizar los 30 forwarders ────────────
MERGE FREIGHT_FORWARDERS AS tgt
USING (VALUES
    ('FF01', 'HILLEBRAND BENELUX',                         'MAASKADE 119, 3071 NK ROTTERDAM',                    NULL,                                         'ROTTERDAM',        'THE NETHERLANDS',  '+31 (0) 104 035 566',       '+31 (0) 104 330 510',   'h.wijngaarde@jfhillebrand.com',           'HARVEY WIJNGAARDE',           'EUR'),
    ('FF02', 'HILLEBRAND ARGENTINA S.A.',                  'SANTA CRUZ 216, CUIT 30-70702857-9',                 'MENDOZA 5500',                               'MENDOZA',          'ARGENTINA',        '54 261 405 7036',           '54 261 405 7000',       'l.portuguez@jfhillebrand.com',            'LEONARDO PORTUGUEZ',          'USD'),
    ('FF03', 'HILLEBRAND CENTRAL EUROPE',                  'WICKENBURGGASSE 26, TOP 4 WIEN 1080',                NULL,                                         'CZECH REPUBLIC',   'CENTRAL EUROPE',   '43 1 52 337 37 17',         '43 1 52 337 37 37',     's.lazarevic@jfhillebrand.com',            'SIMO LAZAREVIC',              'EUR'),
    ('FF04', 'HILLEBRAND GORI CHILE LTDA.',                'ENCOMENDEROS 260, PISO 4 OF. 42-43 LAS CONDES',     NULL,                                         'SANTIAGO',         'CHILE',            '56 228104653',              '56 2 2810 4690',        'paula.chiappa@hillebrandgori.com',         'PAULA CHIAPPA',               'USD'),
    ('FF05', 'HILLEBRAND GORI FRANCE',                     '11 RUE LOUIS ET GASTON CHEVROLET, 21200 VIGNOLES',  NULL,                                         'VIGNOLES',         'FRANCE',           '+33 3802 4 4163',           '+33 3802 4 4399',       'elodie.schaub@hillebrandgori.com',         'ELODIE SCHAUB',               'EUR'),
    ('FF06', 'HILLEBRAND GORI ITALY',                      'VIA VOLTURNO, 10/12- GROMA CENTER, TORRE B-PIANO 2','OSMANNORO, 50019 SESTO FIORENTINO, FL',       'FIORENTINO',       'ITALY',            '+39 055 3415 1278',         '+39(0)5534151235',      'chiara.beatini@hillebrandgori.com',        'Chiara Beatini',              'EUR'),
    ('FF07', 'HILLEBRAND MEXICO',                          'AV. EJERCITO NACIONAL 843-B, PISO 3, COL. GRANADA', 'DEL MIGUEL HIDALGO',                         'CIUDAD DE MEXICO', 'MEXICO',           '52 55 5282 4499 EXT. 420',  '52 55 52 82 45 07',     's.villafuerte@jfhillebrand.com',           'STEPHANIA VILLAFUERTE',       'USD'),
    ('FF08', 'HILLEBRAND GORI PORTUGAL',                   'AV. DOM AFONSO HENRIQUES 1122',                      'SALA (ROOM) G, 4450-011',                    'MATOSINHOS',       'PORTUGAL',         '351 22 939 74 78',          '351 22 938 59 57',      'n.pinheiro@hillebrandgori.com',            'NIDIA PINHEIRO',              'EUR'),
    ('FF09', 'HILLEBRAND SOUTH AFRICA',                    'BOSMAN''S CROSSING, DISTILLERY ROAD',                'STELLENBOSCH 7600',                          'STELLEBOSCH',      'SOUTH AFRICA',     '27 21 809 2000',            '27 21 809 2006',        'h.visser@jfhillebrand.com',               'HEINRICH VISSER',             'USD'),
    ('FF10', 'HILLEBRAND GORI SPAIN, S.A.',                'AVINGUDA PARC LOGISTIC 12-20',                       'OFICINA 1, PLANTA2, EDIFICIO A',             '08040 BARCELONA',  'SPAIN',            '34 933 197 505',            '34 933 106 153',        'gemma.aixa@hillebrandgori.com',            'GEMMA AIXA',                  'EUR'),
    ('FF11', 'HILLEBRAND GORI USA LLC',                    '12621 FEATHERWOOD DRIVE',                            'SUITE 390',                                  'HOUSTON, TX',      'UNITED STATES',    '(707) 935-4370',            NULL,                    'fritzi.escalona@hillebrandgori.com',       'Fritzi Escalona',             'USD'),
    ('FF12', 'AMCAR FREIGHT INC',                          '10100 NW 25 STREET, MIAMI FL 33172',                 NULL,                                         'FLORIDA',          'UNITED STATES',    '(305) 599-8866 EXT 129',    '(305) 599-8532',        'lynette@amcarlamprecht.com',               'LYNETTE QUIROS',              'USD'),
    ('FF13', 'CAVALIER LOGISTICS CARIBBEAN B.V.',          'VEERIS COMMERCIAL PARK 3',                           NULL,                                         'WILLEMSTAD',       'CURACAO',          '599 9 7374321',             '599 9 7377900',         'geronimo.st-hilaire@cavalier.net',         'GERONIMO ST. HILAIRE',        'EUR'),
    ('FF14', 'CURLINE INC',                                'DAMMERS BUILDING, KAYA FLAMBOYAN 11',                'P.O. BOX 3018',                              'WILLEMSTAD',       'CURACAO',          '599-9-737-0600',            '5999-737-3875',         'sales@curline.com',                        'DAVID OGENIA',                'USD'),
    ('FF15', 'ESSEX FREIGHT HOLLAND',                      'KEURMEESTERSTRAAT 16, 2984 BA RIDDERKERK',           NULL,                                         'RIDDERKERK 2984 BA','THE NETHERLANDS', '31 (0) 180 445050',         '31 (0) 180 445054',     'sales@essexfreight.nl',                    'LEO VAN DRIEL',               'EUR'),
    ('FF16', 'ROADJET WAREHOUSE B.V.',                     'SLUISJESDIJK 119',                                   '3087AE',                                     'ROTTERDAM',        'THE NETHERLANDS',  '31 (0) 853001219',          NULL,                    'groupage@roadjet.nl',                      'MARIETTE JESURUN BUQUET',     'EUR'),
    ('FF17', 'FORT INTERNATIONAL, INC',                    '50 BROAD STREET, SUITE 1401, NY 10004',              NULL,                                         'NEW YORK',         'UNITED STATES',    '212-513-6147',              NULL,                    'bdellinger@fortintl.com',                  'ROBERT (BOB) K. DELLINGER',   'USD'),
    ('FF18', 'GOMEZ SHIPPING N.V.',                        'ZEELANDIA Z/N',                                      NULL,                                         'WILLEMSTAD',       'CURACAO',          '+599-9-461-5900',           '+599-9-461-3358',       'luzette@gomezshipping.com',                'LUZETTE L. MERCELINA',        'USD'),
    ('FF19', 'SEL MADURO',                                 'DOKWEG 19',                                          NULL,                                         'WILLEMSTAD',       'CURACAO',          '599-9-733-1543',            '599-9-733-1555',        'cargo@madurosons.com',                     'SHARINE LEMMENS',             'USD'),
    ('FF20', 'SEAWINGS N.V.',                              'MADURO PLAZA',                                       NULL,                                         'WILLEMSTAD',       'CURACAO',          '+5999-733-1536',            '+5999-733-1599',        'gbelioso@madurosons.com',                  'Geanshimayschka Belioso',     'ANG'),
    ('FF21', 'TRANSOLUTION B.V.',                          'DIEPEN 11',                                          '1043 BK',                                    'AMSTERDAM',        'THE NETHERLANDS',  '+31(0)204511160',           '+31(0)618461485',       'dysaina-tcc@transolution.net',             'DYSAINA ELLIOT',              'EUR'),
    ('FF22', 'AMC USA INC.',                               '142 W 57th St',                                      'NY 10019',                                   'NEW YORK',         'USA',              '212-736-1333',              NULL,                    'awollenweber@us-amctransport.com',         'ANTOINE WOLLENWEBER',         'EUR'),
    ('FF23', 'INTEGRITY LOGISTICS & SHIPPING',             '10301 NW 108TH AVE, SUITE#6',                        'MEDLEY, FL 33178',                           'FLORIDA',          'USA',              '305-646-1686',              '800-311-7085',          'alexandra.maya@ictcshipping.com',          'ALEXANDRA MAYA-POLIT',        'USD'),
    ('FF24', 'FLORIDA EXPRESS/PROPAC XPRESS N.V.',         'OUDE CARACASBAAIWEG 99',                             NULL,                                         'WILLEMSTAD',       'CURACAO',          '+5999-465-5277',            NULL,                    'mariette@floridaexpresscuracao.net',       'MARIETTE JESURUN BUQUET',     'ANG'),
    ('FF25', 'Kusters',                                    'Kusters',                                            NULL,                                         'WILLEMSTAD',       'Kusters',          '7376255',                   NULL,                    NULL,                                       NULL,                          'ANG'),
    ('FF26', 'KUEHNE + NAGEL ITALY',                       'VIA DI GONFIENTI 14 D/E',                            'INTERPORTO DELLA TOSCANA CENTRALE',          'IT 59100, PRATO',  'ITALY',            '+39 331 665 9468',          NULL,                    'andrea.lazzeri@kuehne-nagel.com',          'ANDREA LAZZERI',              'EUR'),
    ('FF27', 'IFC INTERNATIONAL FREIGHT CARIBBEAN BV',     'JUPITERWEG 1A',                                      '4782 SE',                                    'MOERDIJK',         'THE NETHERLANDS',  '+31(0)168 409 494',         NULL,                    'dbaartmans@ifcnl.com',                     'DIMPHY BAARTMANS',            'EUR'),
    ('FF28', 'HILLEBRAND SCOTLAND',                        'RIVERSIDE BRAEHEAD RENFREW',                         'STRATHCLYDE REGION PA4 8YU',                 'LONDON',           'SCOTLAND',         'NA',                        NULL,                    NULL,                                       NULL,                          'USD'),
    ('FF29', 'KUEHNE+NAGEL CHILE',                         'AVENIDA APOQUINDO 4501, PISO 14',                    'LAS CONDES, ZIP CODE 7580125',               'SANTIAGO',         'CHILE',            '+56-2 2338 9342',           NULL,                    'gissela.castillo@kuehne-nagel.com',        'GISELLA CASTILLO H.',         'USD'),
    ('FF30', 'MATSON INTEGRATED LOGISTICS',                'P.O. BOX 99074',                                     'IL 60693-0001',                              'CHICAGO',          'USA',              '925-736-2235',              NULL,                    'kcarey@matson.com',                        'KERRY CAREY',                 'USD')
) AS src (FF_CODE, FF_NAME, FF_ADDRESS_1, FF_ADDRESS_2, FF_CITY, FF_COUNTRY, FF_PHONE_1, FF_PHONE_2, FF_EMAIL, FF_CONTACT, FF_CURRENCY)
ON tgt.FF_CODE = src.FF_CODE

WHEN NOT MATCHED BY TARGET THEN
    INSERT (FF_CODE, FF_NAME, FF_ADDRESS_1, FF_ADDRESS_2, FF_CITY, FF_COUNTRY, FF_PHONE_1, FF_PHONE_2, FF_EMAIL, FF_CONTACT, FF_CURRENCY, ISActive, CreatedAt)
    VALUES (src.FF_CODE, src.FF_NAME, src.FF_ADDRESS_1, src.FF_ADDRESS_2, src.FF_CITY, src.FF_COUNTRY, src.FF_PHONE_1, src.FF_PHONE_2, src.FF_EMAIL, src.FF_CONTACT, src.FF_CURRENCY, 1, GETUTCDATE())

WHEN MATCHED THEN
    UPDATE SET
        FF_NAME      = src.FF_NAME,
        FF_ADDRESS_1 = src.FF_ADDRESS_1,
        FF_ADDRESS_2 = src.FF_ADDRESS_2,
        FF_CITY      = src.FF_CITY,
        FF_COUNTRY   = src.FF_COUNTRY,
        FF_PHONE_1   = src.FF_PHONE_1,
        FF_PHONE_2   = src.FF_PHONE_2,
        FF_EMAIL     = src.FF_EMAIL,
        FF_CONTACT   = src.FF_CONTACT,
        FF_CURRENCY  = src.FF_CURRENCY;

PRINT CONCAT('FREIGHT_FORWARDERS seed complete. Rows affected: ', @@ROWCOUNT);
