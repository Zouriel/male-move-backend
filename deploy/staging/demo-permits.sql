-- MALÉ MOVE demo permits: 20 clearly labelled demo roadworks on real named streets in Malé and Hulhumalé.
-- Re-runnable: removes earlier DEMO- permits first.
BEGIN;
DELETE FROM "PermitRoadSegments" WHERE "PermitId" IN (SELECT "Id" FROM "Permits" WHERE "PermitNumber" LIKE 'DEMO-%');
DELETE FROM "PermitAuditEvents" WHERE "PermitId" IN (SELECT "Id" FROM "Permits" WHERE "PermitNumber" LIKE 'DEMO-%');
DELETE FROM "Permits" WHERE "PermitNumber" LIKE 'DEMO-%';
SELECT setseed(0.42);
DO $$
DECLARE
  works text[] := ARRAY['Water main replacement','Sewer line repair','Road resurfacing','Electrical cable laying','Fibre duct installation','Drainage upgrade','Pavement widening','Storm drain cleaning','Street lighting works','Utility trench reinstatement'];
  contractors text[] := ARRAY['Demo Contractor · MWSC works','Demo Contractor · STELCO','Demo Contractor · Ooredoo','Demo Contractor · Dhiraagu','Demo Contractor · HDC','Demo Contractor · Malé City Council'];
  states text[] := ARRAY['Approved','Approved','Approved','Approved','Approved','Approved','Approved','Approved','Approved','Approved','Approved','Approved','Approved','Approved','Approved','Submitted','Submitted','Submitted','NeedsReview','NeedsReview'];
  actor uuid := '00000000-0000-4000-8000-000000000001';
  pick record; pid uuid; i int; island text; impact text; st text; starts timestamptz; ends timestamptz; delay int;
BEGIN
  FOR i IN 1..20 LOOP
    island := CASE WHEN i % 2 = 1 THEN 'Malé' ELSE 'Hulhumalé' END;
    SELECT r."OsmWayId", r."Name", ST_Y(ST_Centroid(r."Geometry")) AS lat INTO pick
    FROM "RoadSegments" r
    WHERE r."Name" IS NOT NULL
      AND r."Name" NOT IN ('Sinamalé Bridge', 'Airport Main Road')
      AND (CASE WHEN island = 'Malé' THEN ST_Y(ST_Centroid(r."Geometry")) < 4.182 ELSE ST_Y(ST_Centroid(r."Geometry")) > 4.205 END)
      AND NOT EXISTS (SELECT 1 FROM "PermitRoadSegments" p JOIN "RoadSegments" q ON q."Id" = p."RoadSegmentId" WHERE q."OsmWayId" = r."OsmWayId")
    ORDER BY random() LIMIT 1;
    st := states[i];
    impact := CASE WHEN i % 7 = 0 AND pick."Name" NOT ILIKE '%Boduthakurufaanu%' AND pick."Name" NOT ILIKE '%Midhili%' THEN 'FullClosure'
                   WHEN i % 3 = 0 THEN 'PartialRestriction' ELSE 'LaneRestriction' END;
    IF st = 'Approved' AND i > 12 THEN
      starts := now() + ((2 + i) || ' days')::interval;            -- upcoming
    ELSE
      starts := now() - ((1 + (i % 5)) || ' days')::interval;      -- in effect now
    END IF;
    ends := starts + ((7 + (i * 3) % 21) || ' days')::interval;
    delay := 45 + (i * 37) % 180;
    pid := gen_random_uuid();
    INSERT INTO "Permits" ("Id","PermitNumber","LocationDescription","WorkDescription","Contractor","ImpactType","ApprovalState","SiteVerificationState","StartsAt","EndsAt","DailyStart","DailyEnd","ReviewComment","ReviewedBy","ReviewedAt","Revision","PermitDocumentId","CreatedAt","UpdatedAt")
    VALUES (pid, format('DEMO-MM-2026-%s', lpad(i::text, 3, '0')), format('%s, %s', pick."Name", island),
            format('[Demo data] %s on %s.', works[1 + (i % array_length(works, 1))], pick."Name"),
            contractors[1 + (i % array_length(contractors, 1))], impact, st,
            CASE WHEN st = 'Approved' THEN 'VerifiedRestricted' ELSE 'Unverified' END,
            starts, ends,
            CASE WHEN i % 4 = 0 THEN time '22:00' END, CASE WHEN i % 4 = 0 THEN time '05:00' END,
            CASE WHEN st = 'Approved' THEN 'Demo approval for the staging workspace.' END,
            CASE WHEN st = 'Approved' THEN actor END, CASE WHEN st = 'Approved' THEN now() - interval '2 days' END,
            1, NULL, now() - ((3 + i) || ' days')::interval, now() - (i || ' hours')::interval);
    INSERT INTO "PermitRoadSegments" ("PermitId","RoadSegmentId","RestrictionType","DelaySeconds","SpeedMultiplier","MatchConfidence","MatchEvidence")
    SELECT pid, r."Id", CASE WHEN impact = 'FullClosure' THEN 'full_closure' ELSE lower(impact) END,
           CASE WHEN impact = 'FullClosure' THEN NULL ELSE delay END, NULL, 1.0, 'Demo data: whole OSM way ' || pick."OsmWayId"
    FROM "RoadSegments" r WHERE r."OsmWayId" = pick."OsmWayId";
    INSERT INTO "PermitAuditEvents" ("Id","PermitId","ActorId","Action","PreviousState","NewState","OccurredAt") VALUES
      (gen_random_uuid(), pid, actor, 'demo_seeded', NULL, st, now());
  END LOOP;
END $$;
SELECT "ApprovalState", "ImpactType", count(*) FROM "Permits" WHERE "PermitNumber" LIKE 'DEMO-%' GROUP BY 1,2 ORDER BY 1,2;
SELECT count(*) AS linked_segments FROM "PermitRoadSegments" WHERE "PermitId" IN (SELECT "Id" FROM "Permits" WHERE "PermitNumber" LIKE 'DEMO-%');
COMMIT;
