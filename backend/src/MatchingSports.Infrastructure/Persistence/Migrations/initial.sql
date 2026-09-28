CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS pgcrypto;
    CREATE EXTENSION IF NOT EXISTS postgis;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE otp_challenges (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        phone_e164 character varying(20) NOT NULL,
        otp_hash char(64) NOT NULL,
        purpose character varying(20) NOT NULL DEFAULT ('LOGIN'),
        attempt_count smallint NOT NULL DEFAULT 0,
        max_attempts smallint NOT NULL DEFAULT 5,
        expires_at timestamp with time zone NOT NULL,
        verified_at timestamp with time zone,
        request_ip inet,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_otp_challenges" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE outbox_messages (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        aggregate_type character varying(60) NOT NULL,
        aggregate_id uuid NOT NULL,
        event_type character varying(100) NOT NULL,
        payload_json jsonb NOT NULL,
        occurred_at timestamp with time zone NOT NULL DEFAULT (now()),
        available_at timestamp with time zone NOT NULL DEFAULT (now()),
        processed_at timestamp with time zone,
        retry_count integer NOT NULL DEFAULT 0,
        last_error text,
        locked_until timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_outbox_messages" PRIMARY KEY (id),
        CONSTRAINT ck_outbox_retry_count CHECK (retry_count >= 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE sports (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        code character varying(32) NOT NULL,
        name character varying(100) NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_sports" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE users (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        phone_e164 character varying(20) NOT NULL,
        phone_verified_at timestamp with time zone,
        status character varying(20) NOT NULL DEFAULT ('ACTIVE'),
        last_login_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_users" PRIMARY KEY (id),
        CONSTRAINT ck_users_phone_e164 CHECK (phone_e164 ~ '^\+[1-9][0-9]{7,14}$')
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE sport_formats (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        sport_id uuid NOT NULL,
        code character varying(32) NOT NULL,
        display_name character varying(100) NOT NULL,
        side_size smallint NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_sport_formats" PRIMARY KEY (id),
        CONSTRAINT ck_sport_formats_side_size CHECK (side_size > 0),
        CONSTRAINT "FK_sport_formats_sports_sport_id" FOREIGN KEY (sport_id) REFERENCES sports (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE admin_actions (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        admin_user_id uuid NOT NULL,
        action_type character varying(40) NOT NULL,
        target_type character varying(30) NOT NULL,
        target_id uuid NOT NULL,
        reason text NOT NULL,
        metadata_json jsonb NOT NULL DEFAULT ('{}'::jsonb),
        correlation_id uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_admin_actions" PRIMARY KEY (id),
        CONSTRAINT "FK_admin_actions_users_admin_user_id" FOREIGN KEY (admin_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE auth_sessions (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        refresh_token_hash char(64) NOT NULL,
        device_info jsonb DEFAULT ('{}'::jsonb),
        ip_address inet,
        user_agent text,
        expires_at timestamp with time zone NOT NULL,
        revoked_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_auth_sessions" PRIMARY KEY (id),
        CONSTRAINT "FK_auth_sessions_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE notifications (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        type character varying(40) NOT NULL,
        title character varying(160) NOT NULL,
        body text NOT NULL,
        entity_type character varying(40),
        entity_id uuid,
        payload_json jsonb NOT NULL DEFAULT ('{}'::jsonb),
        read_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_notifications" PRIMARY KEY (id),
        CONSTRAINT "FK_notifications_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE reports (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        reporter_user_id uuid NOT NULL,
        target_type character varying(20) NOT NULL,
        target_id uuid NOT NULL,
        category character varying(30) NOT NULL,
        description text NOT NULL,
        status character varying(20) NOT NULL DEFAULT ('OPEN'),
        assigned_admin_id uuid,
        resolution text,
        resolved_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_reports" PRIMARY KEY (id),
        CONSTRAINT ck_reports_target CHECK (target_type IN ('USER','LISTING','MATCH')),
        CONSTRAINT "FK_reports_users_assigned_admin_id" FOREIGN KEY (assigned_admin_id) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_reports_users_reporter_user_id" FOREIGN KEY (reporter_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE teams (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        sport_id uuid NOT NULL,
        name character varying(120) NOT NULL,
        captain_user_id uuid NOT NULL,
        description text,
        home_area character varying(150),
        avatar_url text,
        status character varying(20) NOT NULL DEFAULT ('ACTIVE'),
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_teams" PRIMARY KEY (id),
        CONSTRAINT "FK_teams_sports_sport_id" FOREIGN KEY (sport_id) REFERENCES sports (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_teams_users_captain_user_id" FOREIGN KEY (captain_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE user_profiles (
        user_id uuid NOT NULL,
        display_name character varying(100) NOT NULL,
        avatar_url text,
        default_area character varying(150),
        contact_note text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_user_profiles" PRIMARY KEY (user_id),
        CONSTRAINT "FK_user_profiles_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE user_sport_profiles (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        sport_id uuid NOT NULL,
        level character varying(20) NOT NULL DEFAULT ('BEGINNER'),
        preferred_format_codes varchar(32)[],
        bio text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_user_sport_profiles" PRIMARY KEY (id),
        CONSTRAINT "FK_user_sport_profiles_sports_sport_id" FOREIGN KEY (sport_id) REFERENCES sports (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_sport_profiles_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE listings (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        owner_user_id uuid NOT NULL,
        host_team_id uuid,
        sport_id uuid NOT NULL,
        format_id uuid NOT NULL,
        mode character varying(24) NOT NULL,
        title character varying(160) NOT NULL,
        note text,
        level_min character varying(20) NOT NULL DEFAULT ('BEGINNER'),
        level_max character varying(20) NOT NULL DEFAULT ('ADVANCED'),
        open_slots smallint NOT NULL,
        accepted_slots smallint NOT NULL DEFAULT 0,
        start_at timestamp with time zone NOT NULL,
        duration_minutes smallint NOT NULL,
        cutoff_at timestamp with time zone NOT NULL,
        address_text character varying(255) NOT NULL,
        latitude numeric(9,6) NOT NULL,
        longitude numeric(9,6) NOT NULL,
        status character varying(20) NOT NULL DEFAULT ('OPEN'),
        version integer NOT NULL DEFAULT 0,
        cancelled_at timestamp with time zone,
        cancel_reason text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        deleted_at timestamp with time zone,
        location_geography geography (point, 4326),
        CONSTRAINT "PK_listings" PRIMARY KEY (id),
        CONSTRAINT ck_listings_coordinates CHECK (latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180),
        CONSTRAINT ck_listings_slots CHECK (open_slots > 0 AND accepted_slots BETWEEN 0 AND open_slots),
        CONSTRAINT ck_listings_time CHECK (duration_minutes BETWEEN 15 AND 480 AND cutoff_at <= start_at),
        CONSTRAINT "FK_listings_sport_formats_format_id" FOREIGN KEY (format_id) REFERENCES sport_formats (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_listings_sports_sport_id" FOREIGN KEY (sport_id) REFERENCES sports (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_listings_teams_host_team_id" FOREIGN KEY (host_team_id) REFERENCES teams (id) ON DELETE SET NULL,
        CONSTRAINT "FK_listings_users_owner_user_id" FOREIGN KEY (owner_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE team_members (
        team_id uuid NOT NULL,
        user_id uuid NOT NULL,
        role character varying(20) NOT NULL DEFAULT ('MEMBER'),
        status character varying(20) NOT NULL DEFAULT ('ACTIVE'),
        joined_at timestamp with time zone NOT NULL DEFAULT (now()),
        left_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_team_members PRIMARY KEY (team_id, user_id),
        CONSTRAINT "FK_team_members_teams_team_id" FOREIGN KEY (team_id) REFERENCES teams (id) ON DELETE CASCADE,
        CONSTRAINT "FK_team_members_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE listing_requests (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        listing_id uuid NOT NULL,
        applicant_user_id uuid,
        applicant_team_id uuid,
        type character varying(20) NOT NULL,
        requested_slots smallint NOT NULL DEFAULT 1,
        message text,
        status character varying(20) NOT NULL DEFAULT ('PENDING'),
        decided_by_user_id uuid,
        decided_at timestamp with time zone,
        withdrawn_at timestamp with time zone,
        rejection_reason text,
        version integer NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_listing_requests" PRIMARY KEY (id),
        CONSTRAINT ck_listing_requests_applicant CHECK ((type='PLAYER' AND applicant_user_id IS NOT NULL AND applicant_team_id IS NULL) OR (type='OPPONENT' AND applicant_team_id IS NOT NULL AND applicant_user_id IS NULL)),
        CONSTRAINT ck_listing_requests_slots CHECK (requested_slots > 0),
        CONSTRAINT "FK_listing_requests_listings_listing_id" FOREIGN KEY (listing_id) REFERENCES listings (id) ON DELETE CASCADE,
        CONSTRAINT "FK_listing_requests_teams_applicant_team_id" FOREIGN KEY (applicant_team_id) REFERENCES teams (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_listing_requests_users_applicant_user_id" FOREIGN KEY (applicant_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_listing_requests_users_decided_by_user_id" FOREIGN KEY (decided_by_user_id) REFERENCES users (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE matches (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        listing_id uuid NOT NULL,
        status character varying(20) NOT NULL DEFAULT ('PENDING'),
        start_at timestamp with time zone NOT NULL,
        duration_minutes smallint NOT NULL,
        address_text character varying(255) NOT NULL,
        latitude numeric(9,6) NOT NULL,
        longitude numeric(9,6) NOT NULL,
        confirmed_at timestamp with time zone,
        completed_at timestamp with time zone,
        cancelled_at timestamp with time zone,
        cancelled_by_user_id uuid,
        cancel_reason text,
        version integer NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        location_geography geography (point, 4326),
        CONSTRAINT "PK_matches" PRIMARY KEY (id),
        CONSTRAINT "FK_matches_listings_listing_id" FOREIGN KEY (listing_id) REFERENCES listings (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_matches_users_cancelled_by_user_id" FOREIGN KEY (cancelled_by_user_id) REFERENCES users (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE TABLE match_participants (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        match_id uuid NOT NULL,
        user_id uuid,
        team_id uuid,
        source_request_id uuid,
        side_no smallint NOT NULL,
        role character varying(20) NOT NULL,
        status character varying(20) NOT NULL DEFAULT ('CONFIRMED'),
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_match_participants" PRIMARY KEY (id),
        CONSTRAINT ck_match_participants_side CHECK (side_no IN (1,2)),
        CONSTRAINT ck_match_participants_subject CHECK ((user_id IS NOT NULL)::int + (team_id IS NOT NULL)::int = 1),
        CONSTRAINT "FK_match_participants_listing_requests_source_request_id" FOREIGN KEY (source_request_id) REFERENCES listing_requests (id) ON DELETE SET NULL,
        CONSTRAINT "FK_match_participants_matches_match_id" FOREIGN KEY (match_id) REFERENCES matches (id) ON DELETE CASCADE,
        CONSTRAINT "FK_match_participants_teams_team_id" FOREIGN KEY (team_id) REFERENCES teams (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_match_participants_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_admin_actions_admin_user_id" ON admin_actions (admin_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_admin_actions_target ON admin_actions (target_type, target_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_auth_sessions_user_active ON auth_sessions (user_id, expires_at) WHERE revoked_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE UNIQUE INDEX uq_auth_sessions_token_hash ON auth_sessions (refresh_token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_listing_requests_applicant_team_id" ON listing_requests (applicant_team_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_listing_requests_applicant_user_id" ON listing_requests (applicant_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_listing_requests_decided_by_user_id" ON listing_requests (decided_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_listing_requests_listing_status ON listing_requests (listing_id, status, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE UNIQUE INDEX uq_listing_requests_active_team ON listing_requests (listing_id, applicant_team_id) WHERE applicant_team_id IS NOT NULL AND status IN ('PENDING','ACCEPTED');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE UNIQUE INDEX uq_listing_requests_active_user ON listing_requests (listing_id, applicant_user_id) WHERE applicant_user_id IS NOT NULL AND status IN ('PENDING','ACCEPTED');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_listings_expiry ON listings (status, cutoff_at) WHERE status IN ('OPEN','MATCHED');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_listings_feed ON listings (status, sport_id, start_at) WHERE deleted_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_listings_format_id" ON listings (format_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_listings_geo ON listings USING gist (location_geography) WHERE status IN ('OPEN','MATCHED') AND deleted_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_listings_host_team_id" ON listings (host_team_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_listings_owner ON listings (owner_user_id, created_at DESC) WHERE deleted_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_listings_sport_id" ON listings (sport_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_match_participants_source_request_id" ON match_participants (source_request_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_match_participants_team_id" ON match_participants (team_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_match_participants_user_id" ON match_participants (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE UNIQUE INDEX uq_match_participants_team ON match_participants (match_id, team_id) WHERE team_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE UNIQUE INDEX uq_match_participants_user ON match_participants (match_id, user_id) WHERE user_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_matches_cancelled_by_user_id" ON matches (cancelled_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_matches_schedule ON matches (status, start_at) WHERE status IN ('PENDING','CONFIRMED');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE UNIQUE INDEX uq_matches_listing ON matches (listing_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_notifications_unread ON notifications (user_id, created_at DESC) WHERE read_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_otp_challenges_expiry ON otp_challenges (expires_at) WHERE verified_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_otp_challenges_lookup ON otp_challenges (phone_e164, purpose, created_at DESC) WHERE verified_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_outbox_pending ON outbox_messages (available_at, occurred_at) WHERE processed_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_reports_assigned_admin_id" ON reports (assigned_admin_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_reports_queue ON reports (status, created_at) WHERE status IN ('OPEN','IN_REVIEW');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_reports_reporter_user_id" ON reports (reporter_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE UNIQUE INDEX uq_sport_formats_sport_code ON sport_formats (sport_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE UNIQUE INDEX uq_sports_code ON sports (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_team_members_user_status ON team_members (user_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_teams_captain_user_id" ON teams (captain_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX ix_teams_sport_status ON teams (sport_id, status) WHERE deleted_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE INDEX "IX_user_sport_profiles_sport_id" ON user_sport_profiles (sport_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE UNIQUE INDEX uq_user_sport_profiles_user_sport ON user_sport_profiles (user_id, sport_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    CREATE UNIQUE INDEX uq_users_phone_e164 ON users (phone_e164) WHERE deleted_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928063847_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260928063847_InitialCreate', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928160742_HardenDataIntegrity') THEN
    ALTER TABLE listings DROP CONSTRAINT "FK_listings_sport_formats_format_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928160742_HardenDataIntegrity') THEN
    DROP INDEX "IX_listings_format_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928160742_HardenDataIntegrity') THEN
    ALTER TABLE sport_formats ADD CONSTRAINT ak_sport_formats_id_sport_id UNIQUE (id, sport_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928160742_HardenDataIntegrity') THEN
    ALTER TABLE matches ADD CONSTRAINT ck_matches_coordinates CHECK (latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928160742_HardenDataIntegrity') THEN
    ALTER TABLE matches ADD CONSTRAINT ck_matches_time CHECK (duration_minutes BETWEEN 15 AND 480);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928160742_HardenDataIntegrity') THEN
    CREATE INDEX "IX_listings_format_id_sport_id" ON listings (format_id, sport_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928160742_HardenDataIntegrity') THEN
    ALTER TABLE listings ADD CONSTRAINT ck_listings_levels CHECK ((level_min = 'BEGINNER') OR (level_min = 'INTERMEDIATE' AND level_max IN ('INTERMEDIATE','ADVANCED')) OR (level_min = 'ADVANCED' AND level_max = 'ADVANCED'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928160742_HardenDataIntegrity') THEN
    ALTER TABLE listings ADD CONSTRAINT "FK_listings_sport_formats_format_id_sport_id" FOREIGN KEY (format_id, sport_id) REFERENCES sport_formats (id, sport_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928160742_HardenDataIntegrity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260928160742_HardenDataIntegrity', '10.0.4');
    END IF;
END $EF$;
COMMIT;

