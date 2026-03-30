-- ============================================================
-- 35_SeedPortsOfLoading.sql
-- Seed data for PORT_OF_LOADING table
-- ============================================================
select * from PORT_OF_LOADING

USE LicoresMaduoDB;
GO

MERGE INTO PORT_OF_LOADING AS target
USING (VALUES
    ('ANAPH',  'AEROPUERTO HATO',          'CW'),
    ('ANSXM',  'SINT MAARTEN',             'SX'),
    ('ANTBEL', 'ANTWERPEN',                'BE'),
    ('ANTWBE', 'ANTWERPEN',                'BE'),
    ('ANWDW',  'WERF DE WILDE',            'CW'),
    ('ANWEW',  'WEST WERF',                'CW'),
    ('AUA',    'ARUBA',                    'AW'),
    ('AUABAR', 'ARUBA BARCADERA',          'AW'),
    ('BARC',   'BARCELONA',                'ES'),
    ('BQB',    'BONAIRE',                  'BWB'),
    ('BQCOL',  'BQUILLA COLOMBIA',         'CO'),
    ('BQUILL', 'BARRANQUILLA COLOMBIA',    'CO'),
    ('BRAZIL', 'NAVEGANTES PORT BRAZIL',   'BR'),
    ('BRSANT', 'SANTOS',                   'BR'),
    ('BUSAN',  'BUSAN SOUTH KOREA',        'KP'),
    ('BUSKOR', 'BUSAN KOREA SOUTH',        'KP'),
    ('CA',     'BROOKLYN, NY',             'US'),
    ('CANCUN', 'CANCUN MEXICO',            'MX'),
    ('CARCOL', 'CARTAGENA COLOMBIA',       'CO'),
    ('CH',     'CHINA',                    'CN'),
    ('CHANGS', 'CHANGSHA CHINA',           'CN'),
    ('CHINA',  'QINGDAO CHINA',            'CN'),
    ('CHIWAN', 'CHIWAN',                   'CN'),
    ('COLON',  'COLON PANAMA',             'PA'),
    ('DANVIL', 'DANVILLE, VA, USA',        'US'),
    ('ENBRI',  'ENGELAND',                 NULL),
    ('ESBCN',  'BARCELONA',                'ES'),
    ('ESEUR',  'SPANJE',                   'ES'),
    ('FOSHAN', 'FOSHAN CHINA',             'CN'),
    ('FRBAS',  'BASSENS BORDEAUX FRANCE',  'FR'),
    ('FRLEH',  'LE HAVRE FRANCE',          'FR'),
    ('GENOVA', 'GENOVA',                   'IT'),
    ('GOIANI', 'GOIANIA-BRASIL',           'BR'),
    ('HAM',    'HAMBURG',                  'DE'),
    ('HKHKG',  'HONG KONG',               'CN'),
    ('HON',    'PUERTO CORTES HONDURAS',   'HN'),
    ('HOUSTX', 'HOUSTON (TX)',             'US'),
    ('HOUTX',  'HOUSTON TEXAS',            'US'),
    ('INCHON', 'INCHON, KOREA',            'KR'),
    ('INMUN',  'MUNDRA, INDIA',            'IN'),
    ('ITALY',  'ITEUR',                    'IT'),
    ('ITAPOA', 'ITAPOA SC BRAZIL',         'BR'),
    ('ITEUR',  'ITALIE',                   'IT'),
    ('ITRTM',  'ITALIE',                   'IT'),
    ('KANDA',  'KANDA JAPAN',              'JP'),
    ('KINGJ',  'KINGSTON',                 'JM'),
    ('KOJP',   'KOBE JAPAN',               'JP'),
    ('LEESBU', 'LEESBURG FL USA',          'US'),
    ('LIPE',   'LIMA PERU',                'PE'),
    ('LIVORI', 'FRANCE',                   'FR'),
    ('LIVORN', 'LIVORNO',                  'IT'),
    ('NAGOJP', 'NAGOYA JAPAN',             'JP'),
    ('NANSHA', 'NANSHA CHINA',             'CN'),
    ('NHAVA',  'NHAVA SHEVA, INDIA',       'IN'),
    ('NINGBO', 'NINGBO, CHINA',            'CN'),
    ('NLAMS',  'AMSTERDAM',                'NL'),
    ('NLRTM',  'ROTTERDAM',                'NL'),
    ('NYUSA',  'NEW YORK USA',             'US'),
    ('PA',     'PANAMA',                   'PA'),
    ('PERU',   'CALLAO PERU',              'PE'),
    ('POR',    'PORTUGAL',                 'PT'),
    ('PRBRAZ', 'PARANAGUA BRAZIL',         'BR'),
    ('SAN JU', 'PUERTO RICO',              'PR'),
    ('SANSHU', 'SANSHUI CHINA',            'CN'),
    ('SANTOS', 'SANTOS SP BRAZIL',         'BR'),
    ('SAOBRA', 'LAEJADO RS BRAZIL',        'BR'),
    ('SEMA',   'SEMARANG INDONESIA',       'ID'),
    ('SHAGCN', 'SHANGHAI CHINA',           'CN'),
    ('SHEKOU', 'SHEJOU CHINA',             'CN'),
    ('SJ',     'SAN JUAN',                 'PR'),
    ('STODOM', 'SANTO DOMINGO',            'DO'),
    ('SUAPE',  'SUAPE PE BRAZIL',          'BR'),
    ('SUR',    'SURINAME',                 'SR'),
    ('TAICAN', 'TAICANG CHINA',            'CN'),
    ('USA',    'LAS VEGAS',                'US'),
    ('USGEN',  'GENEVA, NY',               'US'),
    ('USMIA',  'MIAMI',                    'US'),
    ('VAL',    'VALENCIA',                 'ES'),
    ('VE',     'VENEZUELA',                'VE'),
    ('VLI',    'VLISSINGEN',               'NL'),
    ('XIAMEN', 'XIAMEN CHINA',             'CN'),
    ('XIN',    'XINGANG',                  'CN'),
    ('YANTIA', 'YANTIAN CHINA',            'CN')
) AS source (PL_CODE, PL_NAME, PL_COUNTRY)
ON target.PL_CODE = source.PL_CODE
WHEN NOT MATCHED THEN
    INSERT (PL_CODE, PL_NAME, PL_COUNTRY, ISActive)
    VALUES (source.PL_CODE, source.PL_NAME, source.PL_COUNTRY, 1);
GO

PRINT 'PORT_OF_LOADING seed completed — ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows inserted.';
GO
