export interface paths {
  '/api/v1/auth/register': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['RegisterRequest'];
          'text/json': components['schemas']['RegisterRequest'];
          'application/*+json': components['schemas']['RegisterRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['RegistrationResponseBaseResponse'];
            'application/json': components['schemas']['RegistrationResponseBaseResponse'];
            'text/json': components['schemas']['RegistrationResponseBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/verify-email': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['EmailOtpRequest'];
          'text/json': components['schemas']['EmailOtpRequest'];
          'application/*+json': components['schemas']['EmailOtpRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ObjectBaseResponse'];
            'application/json': components['schemas']['ObjectBaseResponse'];
            'text/json': components['schemas']['ObjectBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/resend-verification': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['EmailRequest'];
          'text/json': components['schemas']['EmailRequest'];
          'application/*+json': components['schemas']['EmailRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ObjectBaseResponse'];
            'application/json': components['schemas']['ObjectBaseResponse'];
            'text/json': components['schemas']['ObjectBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/login': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['LoginRequest'];
          'text/json': components['schemas']['LoginRequest'];
          'application/*+json': components['schemas']['LoginRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['AuthTokenResponseBaseResponse'];
            'application/json': components['schemas']['AuthTokenResponseBaseResponse'];
            'text/json': components['schemas']['AuthTokenResponseBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/refresh': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['RefreshTokenRequest'];
          'text/json': components['schemas']['RefreshTokenRequest'];
          'application/*+json': components['schemas']['RefreshTokenRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['AuthTokenResponseBaseResponse'];
            'application/json': components['schemas']['AuthTokenResponseBaseResponse'];
            'text/json': components['schemas']['AuthTokenResponseBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/logout': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['RefreshTokenRequest'];
          'text/json': components['schemas']['RefreshTokenRequest'];
          'application/*+json': components['schemas']['RefreshTokenRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ObjectBaseResponse'];
            'application/json': components['schemas']['ObjectBaseResponse'];
            'text/json': components['schemas']['ObjectBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/forgot-password': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['EmailRequest'];
          'text/json': components['schemas']['EmailRequest'];
          'application/*+json': components['schemas']['EmailRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ObjectBaseResponse'];
            'application/json': components['schemas']['ObjectBaseResponse'];
            'text/json': components['schemas']['ObjectBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/reset-password': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['ResetPasswordRequest'];
          'text/json': components['schemas']['ResetPasswordRequest'];
          'application/*+json': components['schemas']['ResetPasswordRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ObjectBaseResponse'];
            'application/json': components['schemas']['ObjectBaseResponse'];
            'text/json': components['schemas']['ObjectBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/google': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content?: never;
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/google/callback': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['AuthTokenResponseBaseResponse'];
            'application/json': components['schemas']['AuthTokenResponseBaseResponse'];
            'text/json': components['schemas']['AuthTokenResponseBaseResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/me': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['CurrentUserResponseBaseResponse'];
            'application/json': components['schemas']['CurrentUserResponseBaseResponse'];
            'text/json': components['schemas']['CurrentUserResponseBaseResponse'];
          };
        };
      };
    };
    put: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['UpdateUserProfileRequest'];
          'text/json': components['schemas']['UpdateUserProfileRequest'];
          'application/*+json': components['schemas']['UpdateUserProfileRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['UserProfileDtoBaseResponse'];
            'application/json': components['schemas']['UserProfileDtoBaseResponse'];
            'text/json': components['schemas']['UserProfileDtoBaseResponse'];
          };
        };
        /** @description Unauthorized */
        401: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ProblemDetails'];
            'application/json': components['schemas']['ProblemDetails'];
            'text/json': components['schemas']['ProblemDetails'];
          };
        };
        /** @description Not Found */
        404: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ProblemDetails'];
            'application/json': components['schemas']['ProblemDetails'];
            'text/json': components['schemas']['ProblemDetails'];
          };
        };
      };
    };
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listing-requests': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          Status?: components['schemas']['RequestStatus'];
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingRequestDtoPaginatedResponse'];
            'application/json': components['schemas']['ListingRequestDtoPaginatedResponse'];
            'text/json': components['schemas']['ListingRequestDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listings/{listingId}/requests': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          Status?: components['schemas']['RequestStatus'];
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path: {
          listingId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingRequestDtoPaginatedResponse'];
            'application/json': components['schemas']['ListingRequestDtoPaginatedResponse'];
            'text/json': components['schemas']['ListingRequestDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          listingId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['JoinListingRequest'];
          'text/json': components['schemas']['JoinListingRequest'];
          'application/*+json': components['schemas']['JoinListingRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingRequestDtoBaseResponse'];
            'application/json': components['schemas']['ListingRequestDtoBaseResponse'];
            'text/json': components['schemas']['ListingRequestDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listings/{listingId}/requests/{requestId}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          listingId: string;
          requestId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingRequestDtoBaseResponse'];
            'application/json': components['schemas']['ListingRequestDtoBaseResponse'];
            'text/json': components['schemas']['ListingRequestDtoBaseResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listings/{listingId}/requests/{requestId}/accept': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          listingId: string;
          requestId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['MatchingVersionRequest'];
          'text/json': components['schemas']['MatchingVersionRequest'];
          'application/*+json': components['schemas']['MatchingVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingRequestDtoBaseResponse'];
            'application/json': components['schemas']['ListingRequestDtoBaseResponse'];
            'text/json': components['schemas']['ListingRequestDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listings/{listingId}/requests/{requestId}/reject': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          listingId: string;
          requestId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['MatchingReasonRequest'];
          'text/json': components['schemas']['MatchingReasonRequest'];
          'application/*+json': components['schemas']['MatchingReasonRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingRequestDtoBaseResponse'];
            'application/json': components['schemas']['ListingRequestDtoBaseResponse'];
            'text/json': components['schemas']['ListingRequestDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listings/{listingId}/requests/{requestId}/withdraw': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          listingId: string;
          requestId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['MatchingVersionRequest'];
          'text/json': components['schemas']['MatchingVersionRequest'];
          'application/*+json': components['schemas']['MatchingVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingRequestDtoBaseResponse'];
            'application/json': components['schemas']['ListingRequestDtoBaseResponse'];
            'text/json': components['schemas']['ListingRequestDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listings': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          SportId?: string;
          FormatId?: string;
          Mode?: components['schemas']['ListingMode'];
          Level?: components['schemas']['SportLevel'];
          StartFrom?: string;
          StartTo?: string;
          Latitude?: number;
          Longitude?: number;
          RadiusMeters?: number;
          MineOnly?: boolean;
          Status?: components['schemas']['ListingStatus'];
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingDtoPaginatedResponse'];
            'application/json': components['schemas']['ListingDtoPaginatedResponse'];
            'text/json': components['schemas']['ListingDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['CreateListingRequest'];
          'text/json': components['schemas']['CreateListingRequest'];
          'application/*+json': components['schemas']['CreateListingRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingDtoBaseResponse'];
            'application/json': components['schemas']['ListingDtoBaseResponse'];
            'text/json': components['schemas']['ListingDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listings/{listingId}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          listingId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingDtoBaseResponse'];
            'application/json': components['schemas']['ListingDtoBaseResponse'];
            'text/json': components['schemas']['ListingDtoBaseResponse'];
          };
        };
      };
    };
    put: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          listingId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['UpdateListingRequest'];
          'text/json': components['schemas']['UpdateListingRequest'];
          'application/*+json': components['schemas']['UpdateListingRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingDtoBaseResponse'];
            'application/json': components['schemas']['ListingDtoBaseResponse'];
            'text/json': components['schemas']['ListingDtoBaseResponse'];
          };
        };
      };
    };
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listings/{listingId}/cancel': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          listingId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['MatchingReasonRequest'];
          'text/json': components['schemas']['MatchingReasonRequest'];
          'application/*+json': components['schemas']['MatchingReasonRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingDtoBaseResponse'];
            'application/json': components['schemas']['ListingDtoBaseResponse'];
            'text/json': components['schemas']['ListingDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listing-watches': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          SportId?: string;
          Status?: components['schemas']['ListingWatchStatus'];
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingWatchDtoPaginatedResponse'];
            'application/json': components['schemas']['ListingWatchDtoPaginatedResponse'];
            'text/json': components['schemas']['ListingWatchDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['ListingWatchRequest'];
          'text/json': components['schemas']['ListingWatchRequest'];
          'application/*+json': components['schemas']['ListingWatchRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingWatchDtoBaseResponse'];
            'application/json': components['schemas']['ListingWatchDtoBaseResponse'];
            'text/json': components['schemas']['ListingWatchDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listing-watches/{watchId}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          watchId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingWatchDtoBaseResponse'];
            'application/json': components['schemas']['ListingWatchDtoBaseResponse'];
            'text/json': components['schemas']['ListingWatchDtoBaseResponse'];
          };
        };
      };
    };
    put: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          watchId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['UpdateListingWatchRequest'];
          'text/json': components['schemas']['UpdateListingWatchRequest'];
          'application/*+json': components['schemas']['UpdateListingWatchRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingWatchDtoBaseResponse'];
            'application/json': components['schemas']['ListingWatchDtoBaseResponse'];
            'text/json': components['schemas']['ListingWatchDtoBaseResponse'];
          };
        };
      };
    };
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listing-watches/{watchId}/pause': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          watchId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['MatchingVersionRequest'];
          'text/json': components['schemas']['MatchingVersionRequest'];
          'application/*+json': components['schemas']['MatchingVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingWatchDtoBaseResponse'];
            'application/json': components['schemas']['ListingWatchDtoBaseResponse'];
            'text/json': components['schemas']['ListingWatchDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listing-watches/{watchId}/activate': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          watchId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['MatchingVersionRequest'];
          'text/json': components['schemas']['MatchingVersionRequest'];
          'application/*+json': components['schemas']['MatchingVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingWatchDtoBaseResponse'];
            'application/json': components['schemas']['ListingWatchDtoBaseResponse'];
            'text/json': components['schemas']['ListingWatchDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/listing-watches/{watchId}/cancel': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          watchId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['MatchingVersionRequest'];
          'text/json': components['schemas']['MatchingVersionRequest'];
          'application/*+json': components['schemas']['MatchingVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ListingWatchDtoBaseResponse'];
            'application/json': components['schemas']['ListingWatchDtoBaseResponse'];
            'text/json': components['schemas']['ListingWatchDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/matches': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          TeamId?: string;
          Status?: components['schemas']['MatchStatus'];
          StartFrom?: string;
          StartTo?: string;
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['MatchDtoPaginatedResponse'];
            'application/json': components['schemas']['MatchDtoPaginatedResponse'];
            'text/json': components['schemas']['MatchDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/matches/{matchId}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          matchId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['MatchDtoBaseResponse'];
            'application/json': components['schemas']['MatchDtoBaseResponse'];
            'text/json': components['schemas']['MatchDtoBaseResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/matches/{matchId}/contact': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          matchId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['MatchContactsDtoBaseResponse'];
            'application/json': components['schemas']['MatchContactsDtoBaseResponse'];
            'text/json': components['schemas']['MatchContactsDtoBaseResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/matches/{matchId}/confirm': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          matchId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['MatchingVersionRequest'];
          'text/json': components['schemas']['MatchingVersionRequest'];
          'application/*+json': components['schemas']['MatchingVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['MatchDtoBaseResponse'];
            'application/json': components['schemas']['MatchDtoBaseResponse'];
            'text/json': components['schemas']['MatchDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/matches/{matchId}/cancel': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          matchId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['MatchingReasonRequest'];
          'text/json': components['schemas']['MatchingReasonRequest'];
          'application/*+json': components['schemas']['MatchingReasonRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['MatchDtoBaseResponse'];
            'application/json': components['schemas']['MatchDtoBaseResponse'];
            'text/json': components['schemas']['MatchDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/admin/matches/{matchId}/cancel': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          matchId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['MatchingReasonRequest'];
          'text/json': components['schemas']['MatchingReasonRequest'];
          'application/*+json': components['schemas']['MatchingReasonRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['MatchDtoBaseResponse'];
            'application/json': components['schemas']['MatchDtoBaseResponse'];
            'text/json': components['schemas']['MatchDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/matches/{matchId}/complete': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          matchId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['MatchingVersionRequest'];
          'text/json': components['schemas']['MatchingVersionRequest'];
          'application/*+json': components['schemas']['MatchingVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['MatchDtoBaseResponse'];
            'application/json': components['schemas']['MatchDtoBaseResponse'];
            'text/json': components['schemas']['MatchDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/notification-configs': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          Status?: string;
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['NotificationConfigDtoPaginatedResponse'];
            'application/json': components['schemas']['NotificationConfigDtoPaginatedResponse'];
            'text/json': components['schemas']['NotificationConfigDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['NotificationConfigRequest'];
          'text/json': components['schemas']['NotificationConfigRequest'];
          'application/*+json': components['schemas']['NotificationConfigRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['NotificationConfigDtoBaseResponse'];
            'application/json': components['schemas']['NotificationConfigDtoBaseResponse'];
            'text/json': components['schemas']['NotificationConfigDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/notification-configs/{id}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          id: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['NotificationConfigDtoBaseResponse'];
            'application/json': components['schemas']['NotificationConfigDtoBaseResponse'];
            'text/json': components['schemas']['NotificationConfigDtoBaseResponse'];
          };
        };
      };
    };
    put: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          id: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['UpdateNotificationConfigRequest'];
          'text/json': components['schemas']['UpdateNotificationConfigRequest'];
          'application/*+json': components['schemas']['UpdateNotificationConfigRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['NotificationConfigDtoBaseResponse'];
            'application/json': components['schemas']['NotificationConfigDtoBaseResponse'];
            'text/json': components['schemas']['NotificationConfigDtoBaseResponse'];
          };
        };
      };
    };
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/notification-configs/{id}/activate': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          id: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['NotificationVersionRequest'];
          'text/json': components['schemas']['NotificationVersionRequest'];
          'application/*+json': components['schemas']['NotificationVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['NotificationConfigDtoBaseResponse'];
            'application/json': components['schemas']['NotificationConfigDtoBaseResponse'];
            'text/json': components['schemas']['NotificationConfigDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/notification-configs/{id}/cancel': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          id: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['NotificationVersionRequest'];
          'text/json': components['schemas']['NotificationVersionRequest'];
          'application/*+json': components['schemas']['NotificationVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['NotificationConfigDtoBaseResponse'];
            'application/json': components['schemas']['NotificationConfigDtoBaseResponse'];
            'text/json': components['schemas']['NotificationConfigDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/notification-configs/preview': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: {
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['NotificationConfigRequest'];
          'text/json': components['schemas']['NotificationConfigRequest'];
          'application/*+json': components['schemas']['NotificationConfigRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['NotificationPreviewDtoBaseResponse'];
            'application/json': components['schemas']['NotificationPreviewDtoBaseResponse'];
            'text/json': components['schemas']['NotificationPreviewDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/notification-configs/{id}/history': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          NotificationConfigId?: string;
          UnreadOnly?: boolean;
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path: {
          id: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['NotificationHistoryDtoPaginatedResponse'];
            'application/json': components['schemas']['NotificationHistoryDtoPaginatedResponse'];
            'text/json': components['schemas']['NotificationHistoryDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/notifications': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          NotificationConfigId?: string;
          UnreadOnly?: boolean;
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['NotificationHistoryDtoPaginatedResponse'];
            'application/json': components['schemas']['NotificationHistoryDtoPaginatedResponse'];
            'text/json': components['schemas']['NotificationHistoryDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/notifications/unread-count': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['UnreadNotificationCountDtoBaseResponse'];
            'application/json': components['schemas']['UnreadNotificationCountDtoBaseResponse'];
            'text/json': components['schemas']['UnreadNotificationCountDtoBaseResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/notifications/{id}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          id: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['NotificationHistoryDtoBaseResponse'];
            'application/json': components['schemas']['NotificationHistoryDtoBaseResponse'];
            'text/json': components['schemas']['NotificationHistoryDtoBaseResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/notifications/{id}/read': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          id: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['NotificationHistoryDtoBaseResponse'];
            'application/json': components['schemas']['NotificationHistoryDtoBaseResponse'];
            'text/json': components['schemas']['NotificationHistoryDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/permissions': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['PermissionDtoPaginatedResponse'];
            'application/json': components['schemas']['PermissionDtoPaginatedResponse'];
            'text/json': components['schemas']['PermissionDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/permissions/{permissionId}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          permissionId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['PermissionDtoBaseResponse'];
            'application/json': components['schemas']['PermissionDtoBaseResponse'];
            'text/json': components['schemas']['PermissionDtoBaseResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/users/{userId}/public-profile': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          userId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['PublicUserProfileDtoBaseResponse'];
            'application/json': components['schemas']['PublicUserProfileDtoBaseResponse'];
            'text/json': components['schemas']['PublicUserProfileDtoBaseResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/public-profile': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['PublicTeamProfileDtoBaseResponse'];
            'application/json': components['schemas']['PublicTeamProfileDtoBaseResponse'];
            'text/json': components['schemas']['PublicTeamProfileDtoBaseResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/roles': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          IsActive?: boolean;
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['RoleDtoPaginatedResponse'];
            'application/json': components['schemas']['RoleDtoPaginatedResponse'];
            'text/json': components['schemas']['RoleDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['CreateRoleRequest'];
          'text/json': components['schemas']['CreateRoleRequest'];
          'application/*+json': components['schemas']['CreateRoleRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['RoleDtoBaseResponse'];
            'application/json': components['schemas']['RoleDtoBaseResponse'];
            'text/json': components['schemas']['RoleDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/roles/{roleId}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          roleId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['RoleDtoBaseResponse'];
            'application/json': components['schemas']['RoleDtoBaseResponse'];
            'text/json': components['schemas']['RoleDtoBaseResponse'];
          };
        };
      };
    };
    put: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          roleId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['UpdateRoleRequest'];
          'text/json': components['schemas']['UpdateRoleRequest'];
          'application/*+json': components['schemas']['UpdateRoleRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['RoleDtoBaseResponse'];
            'application/json': components['schemas']['RoleDtoBaseResponse'];
            'text/json': components['schemas']['RoleDtoBaseResponse'];
          };
        };
      };
    };
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/roles/matrix': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          IsActive?: boolean;
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['RolePermissionMatrixDtoBaseResponse'];
            'application/json': components['schemas']['RolePermissionMatrixDtoBaseResponse'];
            'text/json': components['schemas']['RolePermissionMatrixDtoBaseResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/roles/users/{userId}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          userId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['UserRoleDtoBaseResponse'];
            'application/json': components['schemas']['UserRoleDtoBaseResponse'];
            'text/json': components['schemas']['UserRoleDtoBaseResponse'];
          };
        };
      };
    };
    put: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          userId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['ReplaceUserRolesRequest'];
          'text/json': components['schemas']['ReplaceUserRolesRequest'];
          'application/*+json': components['schemas']['ReplaceUserRolesRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['UserRoleDtoBaseResponse'];
            'application/json': components['schemas']['UserRoleDtoBaseResponse'];
            'text/json': components['schemas']['UserRoleDtoBaseResponse'];
          };
        };
        /** @description Forbidden */
        403: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ProblemDetails'];
            'application/json': components['schemas']['ProblemDetails'];
            'text/json': components['schemas']['ProblemDetails'];
          };
        };
        /** @description Not Found */
        404: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ProblemDetails'];
            'application/json': components['schemas']['ProblemDetails'];
            'text/json': components['schemas']['ProblemDetails'];
          };
        };
      };
    };
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/roles/{roleId}/permissions': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          roleId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['UpdateRolePermissionsRequest'];
          'text/json': components['schemas']['UpdateRolePermissionsRequest'];
          'application/*+json': components['schemas']['UpdateRolePermissionsRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['RoleDtoBaseResponse'];
            'application/json': components['schemas']['RoleDtoBaseResponse'];
            'text/json': components['schemas']['RoleDtoBaseResponse'];
          };
        };
      };
    };
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/sports': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          IsActive?: boolean;
          SportCode?: string;
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['SportDtoPaginatedResponse'];
            'application/json': components['schemas']['SportDtoPaginatedResponse'];
            'text/json': components['schemas']['SportDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['CreateSportRequest'];
          'text/json': components['schemas']['CreateSportRequest'];
          'application/*+json': components['schemas']['CreateSportRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['SportDtoBaseResponse'];
            'application/json': components['schemas']['SportDtoBaseResponse'];
            'text/json': components['schemas']['SportDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/sports/{sportId}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          sportId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['SportDtoBaseResponse'];
            'application/json': components['schemas']['SportDtoBaseResponse'];
            'text/json': components['schemas']['SportDtoBaseResponse'];
          };
        };
      };
    };
    put: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          sportId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['UpdateSportRequest'];
          'text/json': components['schemas']['UpdateSportRequest'];
          'application/*+json': components['schemas']['UpdateSportRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['SportDtoBaseResponse'];
            'application/json': components['schemas']['SportDtoBaseResponse'];
            'text/json': components['schemas']['SportDtoBaseResponse'];
          };
        };
      };
    };
    post?: never;
    delete: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          sportId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['SportDtoBaseResponse'];
            'application/json': components['schemas']['SportDtoBaseResponse'];
            'text/json': components['schemas']['SportDtoBaseResponse'];
          };
        };
      };
    };
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/sports/{sportId}/activate': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          sportId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['SportDtoBaseResponse'];
            'application/json': components['schemas']['SportDtoBaseResponse'];
            'text/json': components['schemas']['SportDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/sports/{sportId}/deactivate': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          sportId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['SportDtoBaseResponse'];
            'application/json': components['schemas']['SportDtoBaseResponse'];
            'text/json': components['schemas']['SportDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ApiInfoResponseBaseResponse'];
            'application/json': components['schemas']['ApiInfoResponseBaseResponse'];
            'text/json': components['schemas']['ApiInfoResponseBaseResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/system-settings': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['SystemSettingsDtoBaseResponse'];
            'application/json': components['schemas']['SystemSettingsDtoBaseResponse'];
            'text/json': components['schemas']['SystemSettingsDtoBaseResponse'];
          };
        };
      };
    };
    put: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['UpdateSystemSettingsRequest'];
          'text/json': components['schemas']['UpdateSystemSettingsRequest'];
          'application/*+json': components['schemas']['UpdateSystemSettingsRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['SystemSettingsDtoBaseResponse'];
            'application/json': components['schemas']['SystemSettingsDtoBaseResponse'];
            'text/json': components['schemas']['SystemSettingsDtoBaseResponse'];
          };
        };
      };
    };
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/invitations': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          Status?: components['schemas']['TeamRequestStatus'];
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path: {
          teamId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamInvitationDtoPaginatedResponse'];
            'application/json': components['schemas']['TeamInvitationDtoPaginatedResponse'];
            'text/json': components['schemas']['TeamInvitationDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['TeamUserRequest'];
          'text/json': components['schemas']['TeamUserRequest'];
          'application/*+json': components['schemas']['TeamUserRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamInvitationDtoBaseResponse'];
            'application/json': components['schemas']['TeamInvitationDtoBaseResponse'];
            'text/json': components['schemas']['TeamInvitationDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/team-invitations': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          Status?: components['schemas']['TeamRequestStatus'];
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamInvitationDtoPaginatedResponse'];
            'application/json': components['schemas']['TeamInvitationDtoPaginatedResponse'];
            'text/json': components['schemas']['TeamInvitationDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/invitations/{invitationId}/cancel': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
          invitationId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['TeamVersionRequest'];
          'text/json': components['schemas']['TeamVersionRequest'];
          'application/*+json': components['schemas']['TeamVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamInvitationDtoBaseResponse'];
            'application/json': components['schemas']['TeamInvitationDtoBaseResponse'];
            'text/json': components['schemas']['TeamInvitationDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/invitations/{invitationId}/accept': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
          invitationId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['TeamVersionRequest'];
          'text/json': components['schemas']['TeamVersionRequest'];
          'application/*+json': components['schemas']['TeamVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamInvitationDtoBaseResponse'];
            'application/json': components['schemas']['TeamInvitationDtoBaseResponse'];
            'text/json': components['schemas']['TeamInvitationDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/invitations/{invitationId}/reject': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
          invitationId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['TeamVersionRequest'];
          'text/json': components['schemas']['TeamVersionRequest'];
          'application/*+json': components['schemas']['TeamVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamInvitationDtoBaseResponse'];
            'application/json': components['schemas']['TeamInvitationDtoBaseResponse'];
            'text/json': components['schemas']['TeamInvitationDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/join-requests': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          Status?: components['schemas']['TeamRequestStatus'];
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path: {
          teamId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamJoinRequestDtoPaginatedResponse'];
            'application/json': components['schemas']['TeamJoinRequestDtoPaginatedResponse'];
            'text/json': components['schemas']['TeamJoinRequestDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['CreateTeamJoinRequest'];
          'text/json': components['schemas']['CreateTeamJoinRequest'];
          'application/*+json': components['schemas']['CreateTeamJoinRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamJoinRequestDtoBaseResponse'];
            'application/json': components['schemas']['TeamJoinRequestDtoBaseResponse'];
            'text/json': components['schemas']['TeamJoinRequestDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/team-join-requests': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          Status?: components['schemas']['TeamRequestStatus'];
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamJoinRequestDtoPaginatedResponse'];
            'application/json': components['schemas']['TeamJoinRequestDtoPaginatedResponse'];
            'text/json': components['schemas']['TeamJoinRequestDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/join-requests/{joinRequestId}/withdraw': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
          joinRequestId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['TeamVersionRequest'];
          'text/json': components['schemas']['TeamVersionRequest'];
          'application/*+json': components['schemas']['TeamVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamJoinRequestDtoBaseResponse'];
            'application/json': components['schemas']['TeamJoinRequestDtoBaseResponse'];
            'text/json': components['schemas']['TeamJoinRequestDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/join-requests/{joinRequestId}/accept': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
          joinRequestId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['TeamVersionRequest'];
          'text/json': components['schemas']['TeamVersionRequest'];
          'application/*+json': components['schemas']['TeamVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamJoinRequestDtoBaseResponse'];
            'application/json': components['schemas']['TeamJoinRequestDtoBaseResponse'];
            'text/json': components['schemas']['TeamJoinRequestDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/join-requests/{joinRequestId}/reject': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
          joinRequestId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['TeamVersionRequest'];
          'text/json': components['schemas']['TeamVersionRequest'];
          'application/*+json': components['schemas']['TeamVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamJoinRequestDtoBaseResponse'];
            'application/json': components['schemas']['TeamJoinRequestDtoBaseResponse'];
            'text/json': components['schemas']['TeamJoinRequestDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          MineOnly?: boolean;
          SportId?: string;
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamDtoPaginatedResponse'];
            'application/json': components['schemas']['TeamDtoPaginatedResponse'];
            'text/json': components['schemas']['TeamDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['CreateTeamRequest'];
          'text/json': components['schemas']['CreateTeamRequest'];
          'application/*+json': components['schemas']['CreateTeamRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamDtoBaseResponse'];
            'application/json': components['schemas']['TeamDtoBaseResponse'];
            'text/json': components['schemas']['TeamDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamDtoBaseResponse'];
            'application/json': components['schemas']['TeamDtoBaseResponse'];
            'text/json': components['schemas']['TeamDtoBaseResponse'];
          };
        };
      };
    };
    put: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['UpdateTeamRequest'];
          'text/json': components['schemas']['UpdateTeamRequest'];
          'application/*+json': components['schemas']['UpdateTeamRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamDtoBaseResponse'];
            'application/json': components['schemas']['TeamDtoBaseResponse'];
            'text/json': components['schemas']['TeamDtoBaseResponse'];
          };
        };
      };
    };
    post?: never;
    delete: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['TeamVersionRequest'];
          'text/json': components['schemas']['TeamVersionRequest'];
          'application/*+json': components['schemas']['TeamVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamDtoBaseResponse'];
            'application/json': components['schemas']['TeamDtoBaseResponse'];
            'text/json': components['schemas']['TeamDtoBaseResponse'];
          };
        };
      };
    };
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/members': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path: {
          teamId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamMemberDtoPaginatedResponse'];
            'application/json': components['schemas']['TeamMemberDtoPaginatedResponse'];
            'text/json': components['schemas']['TeamMemberDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/captain/transfer': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['TransferTeamCaptainRequest'];
          'text/json': components['schemas']['TransferTeamCaptainRequest'];
          'application/*+json': components['schemas']['TransferTeamCaptainRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamDtoBaseResponse'];
            'application/json': components['schemas']['TeamDtoBaseResponse'];
            'text/json': components['schemas']['TeamDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/members/{userId}/deputy': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
          userId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['TeamVersionRequest'];
          'text/json': components['schemas']['TeamVersionRequest'];
          'application/*+json': components['schemas']['TeamVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamDtoBaseResponse'];
            'application/json': components['schemas']['TeamDtoBaseResponse'];
            'text/json': components['schemas']['TeamDtoBaseResponse'];
          };
        };
      };
    };
    delete: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
          userId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['TeamVersionRequest'];
          'text/json': components['schemas']['TeamVersionRequest'];
          'application/*+json': components['schemas']['TeamVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamDtoBaseResponse'];
            'application/json': components['schemas']['TeamDtoBaseResponse'];
            'text/json': components['schemas']['TeamDtoBaseResponse'];
          };
        };
      };
    };
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/members/{userId}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post?: never;
    delete: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
          userId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['TeamVersionRequest'];
          'text/json': components['schemas']['TeamVersionRequest'];
          'application/*+json': components['schemas']['TeamVersionRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamDtoBaseResponse'];
            'application/json': components['schemas']['TeamDtoBaseResponse'];
            'text/json': components['schemas']['TeamDtoBaseResponse'];
          };
        };
      };
    };
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/teams/{teamId}/leave': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          teamId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['TeamDtoBaseResponse'];
            'application/json': components['schemas']['TeamDtoBaseResponse'];
            'text/json': components['schemas']['TeamDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/me/preferences': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['UserPreferencesDtoBaseResponse'];
            'application/json': components['schemas']['UserPreferencesDtoBaseResponse'];
            'text/json': components['schemas']['UserPreferencesDtoBaseResponse'];
          };
        };
      };
    };
    put: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['UpdateUserPreferencesRequest'];
          'text/json': components['schemas']['UpdateUserPreferencesRequest'];
          'application/*+json': components['schemas']['UpdateUserPreferencesRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['UserPreferencesDtoBaseResponse'];
            'application/json': components['schemas']['UserPreferencesDtoBaseResponse'];
            'text/json': components['schemas']['UserPreferencesDtoBaseResponse'];
          };
        };
      };
    };
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/users': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: {
          Status?: components['schemas']['UserStatus'];
          Id?: string;
          SearchString?: string;
          FromDate?: string;
          ToDate?: string;
          PageNumber?: number;
          PageSize?: number;
        };
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['UserListDtoPaginatedResponse'];
            'application/json': components['schemas']['UserListDtoPaginatedResponse'];
            'text/json': components['schemas']['UserListDtoPaginatedResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/user-sport-profiles': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['CreateUserSportProfileRequest'];
          'text/json': components['schemas']['CreateUserSportProfileRequest'];
          'application/*+json': components['schemas']['CreateUserSportProfileRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['UserSportProfileDtoBaseResponse'];
            'application/json': components['schemas']['UserSportProfileDtoBaseResponse'];
            'text/json': components['schemas']['UserSportProfileDtoBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/user-sport-profiles/{profileId}': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          profileId: string;
        };
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['UpdateUserSportProfileRequest'];
          'text/json': components['schemas']['UpdateUserSportProfileRequest'];
          'application/*+json': components['schemas']['UpdateUserSportProfileRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['UserSportProfileDtoBaseResponse'];
            'application/json': components['schemas']['UserSportProfileDtoBaseResponse'];
            'text/json': components['schemas']['UserSportProfileDtoBaseResponse'];
          };
        };
      };
    };
    post?: never;
    delete: {
      parameters: {
        query?: never;
        header?: never;
        path: {
          profileId: string;
        };
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ObjectBaseResponse'];
            'application/json': components['schemas']['ObjectBaseResponse'];
            'text/json': components['schemas']['ObjectBaseResponse'];
          };
        };
      };
    };
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/web/csrf': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['WebCsrfResponseBaseResponse'];
            'application/json': components['schemas']['WebCsrfResponseBaseResponse'];
            'text/json': components['schemas']['WebCsrfResponseBaseResponse'];
          };
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/web/login': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: {
        content: {
          'application/json': components['schemas']['LoginRequest'];
          'text/json': components['schemas']['LoginRequest'];
          'application/*+json': components['schemas']['LoginRequest'];
        };
      };
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['WebAuthTokenResponseBaseResponse'];
            'application/json': components['schemas']['WebAuthTokenResponseBaseResponse'];
            'text/json': components['schemas']['WebAuthTokenResponseBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/web/refresh': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['WebAuthTokenResponseBaseResponse'];
            'application/json': components['schemas']['WebAuthTokenResponseBaseResponse'];
            'text/json': components['schemas']['WebAuthTokenResponseBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/web/logout': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get?: never;
    put?: never;
    post: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content: {
            'text/plain': components['schemas']['ObjectBaseResponse'];
            'application/json': components['schemas']['ObjectBaseResponse'];
            'text/json': components['schemas']['ObjectBaseResponse'];
          };
        };
      };
    };
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/web/google': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content?: never;
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
  '/api/v1/auth/web/google/callback': {
    parameters: {
      query?: never;
      header?: never;
      path?: never;
      cookie?: never;
    };
    get: {
      parameters: {
        query?: never;
        header?: never;
        path?: never;
        cookie?: never;
      };
      requestBody?: never;
      responses: {
        /** @description OK */
        200: {
          headers: {
            [name: string]: unknown;
          };
          content?: never;
        };
      };
    };
    put?: never;
    post?: never;
    delete?: never;
    options?: never;
    head?: never;
    patch?: never;
    trace?: never;
  };
}
export type webhooks = Record<string, never>;
export interface components {
  schemas: {
    ApiInfoResponse: {
      service?: string | null;
      version?: string | null;
      database?: string | null;
    };
    ApiInfoResponseBaseResponse: {
      data: components['schemas']['ApiInfoResponse'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    AuthTokenResponse: {
      accessToken?: string | null;
      refreshToken?: string | null;
      tokenType?: string | null;
      /** Format: date-time */
      accessTokenExpiresAt?: string;
      /** Format: date-time */
      refreshTokenExpiresAt?: string;
      user?: components['schemas']['CurrentUserResponse'];
    };
    AuthTokenResponseBaseResponse: {
      data: components['schemas']['AuthTokenResponse'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    CreateListingRequest: {
      /** Format: uuid */
      sportId: string;
      /** Format: uuid */
      formatId: string;
      /** Format: uuid */
      hostTeamId?: string | null;
      mode: components['schemas']['ListingMode'];
      title: string;
      description?: string | null;
      note?: string | null;
      levelMin?: components['schemas']['SportLevel'];
      levelMax?: components['schemas']['SportLevel'];
      /** Format: int32 */
      openSlots: number;
      /** Format: date-time */
      startAt: string;
      /** Format: int32 */
      durationMinutes: number;
      /** Format: date-time */
      cutoffAt: string;
      addressText: string;
      /** Format: double */
      latitude: number;
      /** Format: double */
      longitude: number;
    };
    CreateRoleRequest: {
      name: string;
      description?: string | null;
      isDefaultForRegistration?: boolean;
    };
    CreateSportRequest: {
      code: string;
      name: string;
      description?: string | null;
      formats: components['schemas']['SportFormatRequest'][];
    };
    CreateTeamJoinRequest: {
      message?: string | null;
    };
    CreateTeamRequest: {
      name: string;
      description?: string | null;
      homeArea?: string | null;
      avatarUrl?: string | null;
      /** Format: uuid */
      sportId?: string;
    };
    CreateUserSportProfileRequest: {
      /** Format: uuid */
      sportId: string;
      level: string;
      preferredFormatIds: string[];
      bio?: string | null;
    };
    CurrentUserResponse: {
      /** Format: uuid */
      id?: string;
      email?: string | null;
      displayName?: string | null;
      emailVerified?: boolean;
      roles?: string[] | null;
      permissions?: string[] | null;
      sportProfiles?: components['schemas']['UserSportProfileDto'][] | null;
    };
    CurrentUserResponseBaseResponse: {
      data: components['schemas']['CurrentUserResponse'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    EmailOtpRequest: {
      /** Format: email */
      email: string;
      otp: string;
    };
    EmailRequest: {
      /** Format: email */
      email: string;
    };
    JoinListingRequest: {
      /** Format: uuid */
      applicantTeamId?: string | null;
      message?: string | null;
    };
    ListingDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      concurrencyToken?: string;
      /** Format: uuid */
      id?: string;
      /** Format: uuid */
      ownerUserId?: string;
      /** Format: uuid */
      hostTeamId?: string | null;
      /** Format: uuid */
      sportId?: string;
      /** Format: uuid */
      formatId?: string;
      mode?: components['schemas']['ListingMode'];
      title?: string | null;
      description?: string | null;
      note?: string | null;
      levelMin?: components['schemas']['SportLevel'];
      levelMax?: components['schemas']['SportLevel'];
      /** Format: int32 */
      openSlots?: number;
      /** Format: int32 */
      acceptedSlots?: number;
      /** Format: date-time */
      startAt?: string;
      /** Format: date-time */
      cutoffAt?: string;
      /** Format: int32 */
      durationMinutes?: number;
      addressText?: string | null;
      /** Format: double */
      latitude?: number;
      /** Format: double */
      longitude?: number;
      status?: components['schemas']['ListingStatus'];
      /** Format: date-time */
      cancelledAt?: string | null;
      cancelReason?: string | null;
      /** Format: uuid */
      matchId?: string | null;
      /** Format: double */
      distanceMeters?: number | null;
    };
    ListingDtoBaseResponse: {
      data: components['schemas']['ListingDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    ListingDtoPaginatedResponse: {
      data: components['schemas']['ListingDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    /** @enum {string} */
    ListingMode: 'FIND_OPPONENT' | 'FIND_PLAYERS';
    ListingRequestDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      concurrencyToken?: string;
      /** Format: uuid */
      id?: string;
      /** Format: uuid */
      listingId?: string;
      /** Format: uuid */
      applicantUserId?: string | null;
      /** Format: uuid */
      applicantTeamId?: string | null;
      type?: components['schemas']['RequestType'];
      /** Format: int32 */
      requestedSlots?: number;
      message?: string | null;
      status?: components['schemas']['RequestStatus'];
      /** Format: uuid */
      decidedByUserId?: string | null;
      /** Format: date-time */
      decidedAt?: string | null;
      /** Format: date-time */
      withdrawnAt?: string | null;
      rejectionReason?: string | null;
    };
    ListingRequestDtoBaseResponse: {
      data: components['schemas']['ListingRequestDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    ListingRequestDtoPaginatedResponse: {
      data: components['schemas']['ListingRequestDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    /** @enum {string} */
    ListingStatus:
      | 'DRAFT'
      | 'OPEN'
      | 'MATCHED'
      | 'CONFIRMED'
      | 'COMPLETED'
      | 'CANCELLED'
      | 'EXPIRED';
    ListingWatchDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      concurrencyToken?: string;
      /** Format: uuid */
      id?: string;
      /** Format: uuid */
      sportId?: string;
      /** Format: uuid */
      formatId?: string | null;
      /** Format: double */
      latitude?: number;
      /** Format: double */
      longitude?: number;
      /** Format: int32 */
      radiusMeters?: number;
      /** Format: date-time */
      startFrom?: string;
      /** Format: date-time */
      startTo?: string;
      status?: components['schemas']['ListingWatchStatus'];
      emailEnabled?: boolean;
    };
    ListingWatchDtoBaseResponse: {
      data: components['schemas']['ListingWatchDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    ListingWatchDtoPaginatedResponse: {
      data: components['schemas']['ListingWatchDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    ListingWatchRequest: {
      /** Format: uuid */
      sportId: string;
      /** Format: uuid */
      formatId?: string | null;
      /** Format: double */
      latitude: number;
      /** Format: double */
      longitude: number;
      /** Format: int32 */
      radiusMeters: number;
      /** Format: date-time */
      startFrom: string;
      /** Format: date-time */
      startTo: string;
      emailEnabled?: boolean;
    };
    /** @enum {string} */
    ListingWatchStatus: 'ACTIVE' | 'PAUSED' | 'CANCELLED' | 'EXPIRED';
    LoginRequest: {
      /** Format: email */
      email: string;
      password: string;
    };
    MatchContactDto: {
      /** Format: uuid */
      userId?: string;
      displayName?: string | null;
      contactNote?: string | null;
    };
    MatchContactsDto: {
      /** Format: uuid */
      matchId?: string;
      contacts?: components['schemas']['MatchContactDto'][] | null;
    };
    MatchContactsDtoBaseResponse: {
      data: components['schemas']['MatchContactsDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    MatchDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      concurrencyToken?: string;
      /** Format: uuid */
      id?: string;
      /** Format: uuid */
      listingId?: string;
      status?: components['schemas']['MatchStatus'];
      /** Format: date-time */
      startAt?: string;
      /** Format: int32 */
      durationMinutes?: number;
      addressText?: string | null;
      /** Format: double */
      latitude?: number;
      /** Format: double */
      longitude?: number;
      /** Format: date-time */
      confirmedAt?: string | null;
      /** Format: date-time */
      completedAt?: string | null;
      /** Format: date-time */
      cancelledAt?: string | null;
      /** Format: uuid */
      cancelledByUserId?: string | null;
      cancelReason?: string | null;
      participants?: components['schemas']['MatchParticipantDto'][] | null;
    };
    MatchDtoBaseResponse: {
      data: components['schemas']['MatchDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    MatchDtoPaginatedResponse: {
      data: components['schemas']['MatchDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    MatchParticipantDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      id?: string;
      /** Format: uuid */
      matchId?: string;
      /** Format: uuid */
      userId?: string | null;
      /** Format: uuid */
      teamId?: string | null;
      /** Format: uuid */
      sourceRequestId?: string | null;
      /** Format: int32 */
      sideNo?: number;
      role?: components['schemas']['ParticipantRole'];
      status?: components['schemas']['ParticipantStatus'];
    };
    /** @enum {string} */
    MatchStatus: 'PENDING' | 'CONFIRMED' | 'COMPLETED' | 'CANCELLED';
    MatchingReasonRequest: {
      /** Format: uuid */
      concurrencyToken: string;
      reason: string;
    };
    MatchingVersionRequest: {
      /** Format: uuid */
      concurrencyToken: string;
    };
    /** @enum {string} */
    MemberStatus: 'ACTIVE' | 'LEFT' | 'REMOVED';
    /** @enum {string} */
    NotificationAudienceType: 'SELECTED_USERS' | 'SELECTED_TEAMS' | 'ALL_USERS';
    NotificationConfigDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      concurrencyToken?: string;
      description?: string | null;
      /** Format: uuid */
      id?: string;
      isSendNow?: boolean;
      scheduleType?: components['schemas']['NotificationScheduleType'];
      cronString?: string | null;
      critical?: components['schemas']['NotificationCritical'];
      title?: string | null;
      body?: string | null;
      bodyText?: string | null;
      /** Format: date-time */
      availableAt?: string;
      /** Format: date-time */
      expiredAfter?: string | null;
      /** Format: int32 */
      maxSendTimes?: number | null;
      audienceType?: components['schemas']['NotificationAudienceType'];
      userIds?: string[] | null;
      teamIds?: string[] | null;
      inAppEnabled?: boolean;
      emailEnabled?: boolean;
      pushEnabled?: boolean;
      smsEnabled?: boolean;
      metadata?: unknown;
      timeZoneId?: string | null;
      status?: components['schemas']['NotificationConfigStatus'];
      schedulingStatus?: components['schemas']['NotificationSchedulingStatus'];
      /** Format: date-time */
      nextRunAt?: string | null;
      /** Format: date-time */
      lastRunAt?: string | null;
      /** Format: int32 */
      sendCount?: number;
      /** Format: date-time */
      lockedUntil?: string | null;
      abortReason?: string | null;
    };
    NotificationConfigDtoBaseResponse: {
      data: components['schemas']['NotificationConfigDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    NotificationConfigDtoPaginatedResponse: {
      data: components['schemas']['NotificationConfigDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    NotificationConfigRequest: {
      description?: string | null;
      isSendNow?: boolean;
      scheduleType: string;
      cronString?: string | null;
      critical: string;
      title: string;
      body: string;
      /** Format: date-time */
      availableAt?: string;
      /** Format: date-time */
      expiredAfter?: string | null;
      /** Format: int32 */
      maxSendTimes?: number | null;
      audienceType: string;
      userIds?: string[] | null;
      teamIds?: string[] | null;
      inAppEnabled?: boolean;
      emailEnabled?: boolean;
      pushEnabled?: boolean;
      smsEnabled?: boolean;
      metadata?: unknown;
      timeZoneId: string;
    };
    /** @enum {string} */
    NotificationConfigStatus: 'DRAFT' | 'ACTIVE' | 'CANCEL';
    /** @enum {string} */
    NotificationCritical: 'INFO' | 'WARN' | 'CRIT';
    /** @enum {string} */
    NotificationDeliveryStatus:
      'DISABLED' | 'PENDING' | 'SENT' | 'FAILED' | 'SKIPPED';
    NotificationHistoryDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      id?: string;
      /** Format: uuid */
      notificationConfigId?: string | null;
      /** Format: uuid */
      userId?: string | null;
      /** Format: uuid */
      teamId?: string | null;
      /** Format: int32 */
      sendNumber?: number;
      /** Format: date-time */
      scheduledAt?: string;
      /** Format: date-time */
      expiresAt?: string | null;
      /** Format: date-time */
      readAt?: string | null;
      /** Format: date-time */
      purgeAfter?: string;
      type?: components['schemas']['NotificationType'];
      critical?: components['schemas']['NotificationCritical'];
      title?: string | null;
      body?: string | null;
      bodyText?: string | null;
      entityType?: string | null;
      /** Format: uuid */
      entityId?: string | null;
      metadata?: unknown;
      inAppEnabled?: boolean;
      emailStatus?: components['schemas']['NotificationDeliveryStatus'];
      pushStatus?: components['schemas']['NotificationDeliveryStatus'];
      smsStatus?: components['schemas']['NotificationDeliveryStatus'];
      /** Format: date-time */
      emailSentAt?: string | null;
      /** Format: date-time */
      pushSentAt?: string | null;
      /** Format: date-time */
      smsSentAt?: string | null;
      emailErrorCode?: string | null;
      pushErrorCode?: string | null;
      smsErrorCode?: string | null;
    };
    NotificationHistoryDtoBaseResponse: {
      data: components['schemas']['NotificationHistoryDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    NotificationHistoryDtoPaginatedResponse: {
      data: components['schemas']['NotificationHistoryDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    NotificationPreviewDto: {
      body?: string | null;
      bodyText?: string | null;
      nextOccurrences?: string[] | null;
      recipients?: components['schemas']['NotificationRecipientDtoPaginatedResult'];
    };
    NotificationPreviewDtoBaseResponse: {
      data: components['schemas']['NotificationPreviewDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    NotificationRecipientDto: {
      /** Format: uuid */
      userId?: string;
      sourceTeamIds?: string[] | null;
    };
    NotificationRecipientDtoPaginatedResult: {
      items?: components['schemas']['NotificationRecipientDto'][] | null;
      /** Format: int32 */
      pageNumber?: number;
      /** Format: int32 */
      pageSize?: number;
      /** Format: int64 */
      totalItems?: number;
      /** Format: int64 */
      readonly totalPages?: number;
      readonly hasPreviousPage?: boolean;
      readonly hasNextPage?: boolean;
    };
    /** @enum {string} */
    NotificationScheduleType: 'ONCE' | 'SCHEDULED';
    /** @enum {string} */
    NotificationSchedulingStatus:
      'PENDING' | 'SCHEDULING' | 'COMPLETED' | 'ABORTED';
    /** @enum {string} */
    NotificationType:
      | 'ADMIN_MESSAGE'
      | 'REQUEST_CREATED'
      | 'REQUEST_ACCEPTED'
      | 'REQUEST_REJECTED'
      | 'LISTING_UPDATED'
      | 'MATCH_CANCELLED'
      | 'MATCH_REMINDER'
      | 'REPORT_RESOLVED'
      | 'TEAM_CREATED'
      | 'TEAM_UPDATED'
      | 'TEAM_INVITATION_CREATED'
      | 'TEAM_INVITATION_CANCELLED'
      | 'TEAM_INVITATION_ACCEPTED'
      | 'TEAM_INVITATION_REJECTED'
      | 'TEAM_DEPUTY_APPOINTED'
      | 'TEAM_DEPUTY_REVOKED'
      | 'TEAM_JOIN_REQUEST_CREATED'
      | 'TEAM_JOIN_REQUEST_WITHDRAWN'
      | 'TEAM_JOIN_REQUEST_ACCEPTED'
      | 'TEAM_JOIN_REQUEST_REJECTED'
      | 'TEAM_JOIN_REQUEST_CANCELLED'
      | 'TEAM_MEMBER_LEFT'
      | 'TEAM_MEMBER_REMOVED'
      | 'TEAM_CAPTAIN_TRANSFERRED'
      | 'TEAM_DELETED'
      | 'REQUEST_WITHDRAWN'
      | 'REQUEST_EXPIRED'
      | 'LISTING_CANCELLED'
      | 'MATCH_CONFIRMED'
      | 'MATCH_COMPLETED'
      | 'LISTING_WATCH_MATCHED';
    NotificationVersionRequest: {
      /** Format: uuid */
      concurrencyToken?: string;
    };
    ObjectBaseResponse: {
      data: unknown;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    PaginationMetadata: {
      /** Format: int32 */
      pageNumber: number;
      /** Format: int32 */
      pageSize: number;
      /** Format: int64 */
      totalItems: number;
      /** Format: int64 */
      totalPages: number;
      hasPreviousPage: boolean;
      hasNextPage: boolean;
    };
    /** @enum {string} */
    ParticipantRole: 'HOST' | 'OPPONENT' | 'PLAYER';
    /** @enum {string} */
    ParticipantStatus: 'CONFIRMED' | 'CANCELLED' | 'NO_SHOW' | 'COMPLETED';
    PermissionDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      id?: string;
      code?: string | null;
      name?: string | null;
      description?: string | null;
    };
    PermissionDtoBaseResponse: {
      data: components['schemas']['PermissionDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    PermissionDtoPaginatedResponse: {
      data: components['schemas']['PermissionDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    ProblemDetails: {
      type?: string | null;
      title?: string | null;
      /** Format: int32 */
      status?: number | null;
      detail?: string | null;
      instance?: string | null;
    } & {
      [key: string]: unknown;
    };
    PublicFormatDto: {
      /** Format: uuid */
      formatId?: string;
      code?: string | null;
      displayName?: string | null;
      /** Format: int32 */
      sideSize?: number;
    };
    PublicSportProfileDto: {
      sport?: components['schemas']['PublicSportSummaryDto'];
      level?: components['schemas']['SportLevel'];
      bio?: string | null;
      preferredFormats?: components['schemas']['PublicFormatDto'][] | null;
    };
    PublicSportSummaryDto: {
      /** Format: uuid */
      sportId?: string;
      code?: string | null;
      name?: string | null;
    };
    PublicTeamProfileDto: {
      /** Format: uuid */
      teamId?: string;
      name?: string | null;
      description?: string | null;
      avatarUrl?: string | null;
      homeArea?: string | null;
      sport?: components['schemas']['PublicSportSummaryDto'];
      captain?: components['schemas']['PublicUserSummaryDto'];
      /** Format: int32 */
      activeMemberCount?: number;
    };
    PublicTeamProfileDtoBaseResponse: {
      data: components['schemas']['PublicTeamProfileDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    PublicUserProfileDto: {
      /** Format: uuid */
      userId?: string;
      displayName?: string | null;
      avatarUrl?: string | null;
      defaultArea?: string | null;
      sportProfiles?: components['schemas']['PublicSportProfileDto'][] | null;
    };
    PublicUserProfileDtoBaseResponse: {
      data: components['schemas']['PublicUserProfileDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    PublicUserSummaryDto: {
      /** Format: uuid */
      userId?: string;
      displayName?: string | null;
      avatarUrl?: string | null;
    };
    RefreshTokenRequest: {
      refreshToken: string;
    };
    RegisterRequest: {
      /** Format: email */
      email: string;
      password: string;
      displayName: string;
    };
    RegistrationResponse: {
      /** Format: uuid */
      userId?: string;
      email?: string | null;
      /** Format: date-time */
      otpExpiresAt?: string;
    };
    RegistrationResponseBaseResponse: {
      data: components['schemas']['RegistrationResponse'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    ReplaceUserRolesRequest: {
      roleIds: string[];
      concurrencyStamp: string;
    };
    /** @enum {string} */
    RequestStatus:
      | 'PENDING'
      | 'ACCEPTED'
      | 'REJECTED'
      | 'WITHDRAWN'
      | 'EXPIRED'
      | 'CANCELLED';
    /** @enum {string} */
    RequestType: 'PLAYER' | 'OPPONENT';
    ResetPasswordRequest: {
      /** Format: email */
      email: string;
      otp: string;
      newPassword: string;
    };
    RoleDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      id?: string;
      name?: string | null;
      description?: string | null;
      isActive?: boolean;
      isDefaultForRegistration?: boolean;
      concurrencyStamp?: string | null;
      permissions?: components['schemas']['PermissionDto'][] | null;
    };
    RoleDtoBaseResponse: {
      data: components['schemas']['RoleDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    RoleDtoPaginatedResponse: {
      data: components['schemas']['RoleDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    RoleDtoPaginatedResult: {
      items?: components['schemas']['RoleDto'][] | null;
      /** Format: int32 */
      pageNumber?: number;
      /** Format: int32 */
      pageSize?: number;
      /** Format: int64 */
      totalItems?: number;
      /** Format: int64 */
      readonly totalPages?: number;
      readonly hasPreviousPage?: boolean;
      readonly hasNextPage?: boolean;
    };
    RolePermissionMatrixDto: {
      permissions?: components['schemas']['PermissionDto'][] | null;
      roles?: components['schemas']['RoleDtoPaginatedResult'];
    };
    RolePermissionMatrixDtoBaseResponse: {
      data: components['schemas']['RolePermissionMatrixDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    SportDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      description?: string | null;
      /** Format: uuid */
      id?: string;
      code?: string | null;
      name?: string | null;
      isActive?: boolean;
      formats?: components['schemas']['SportFormatDto'][] | null;
    };
    SportDtoBaseResponse: {
      data: components['schemas']['SportDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    SportDtoPaginatedResponse: {
      data: components['schemas']['SportDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    SportFormatDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      description?: string | null;
      /** Format: uuid */
      id?: string;
      code?: string | null;
      displayName?: string | null;
      isActive?: boolean;
      /** Format: int32 */
      sideSize?: number;
    };
    SportFormatRequest: {
      /** Format: uuid */
      id?: string | null;
      action: string;
      description?: string | null;
      code?: string | null;
      displayName?: string | null;
      /** Format: int32 */
      sideSize?: number;
    };
    /** @enum {string} */
    SportLevel: 'BEGINNER' | 'INTERMEDIATE' | 'ADVANCED';
    SystemSettingsDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      concurrencyToken?: string;
      /** Format: int32 */
      outboxMaxRetryCount?: number;
      /** Format: int32 */
      maxCaptainTeamsPerUser?: number;
      /** Format: int32 */
      maxMemberTeamsPerUser?: number;
      /** Format: int32 */
      maxDeputiesPerTeam?: number;
      /** Format: int32 */
      maxMembersPerTeam?: number;
      /** Format: int32 */
      teamInvitationLifetimeHours?: number;
      /** Format: int32 */
      maxPendingTeamRequests?: number;
      /** Format: int32 */
      outboxRetryInitialDelaySeconds?: number;
      /** Format: int32 */
      outboxRetryMaxDelaySeconds?: number;
      /** Format: int32 */
      processedOutboxRetentionDays?: number;
      /** Format: int32 */
      notificationRetentionDays?: number;
      /** Format: int32 */
      maxActiveListingWatchesPerUser?: number;
      /** Format: int32 */
      maxListingSearchRadiusMeters?: number;
      /** Format: int32 */
      maxListingWatchWindowDays?: number;
      /** Format: int32 */
      matchReminderLeadMinutes?: number;
    };
    SystemSettingsDtoBaseResponse: {
      data: components['schemas']['SystemSettingsDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    TeamDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      concurrencyToken?: string;
      /** Format: uuid */
      id?: string;
      /** Format: uuid */
      sportId?: string;
      name?: string | null;
      /** Format: uuid */
      captainUserId?: string;
      description?: string | null;
      homeArea?: string | null;
      avatarUrl?: string | null;
      status?: components['schemas']['TeamStatus'];
      /** Format: date-time */
      deletedAt?: string | null;
    };
    TeamDtoBaseResponse: {
      data: components['schemas']['TeamDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    TeamDtoPaginatedResponse: {
      data: components['schemas']['TeamDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    TeamInvitationDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      concurrencyToken?: string;
      /** Format: uuid */
      id?: string;
      /** Format: uuid */
      teamId?: string;
      /** Format: uuid */
      userId?: string;
      /** Format: uuid */
      inviterUserId?: string;
      status?: components['schemas']['TeamRequestStatus'];
      /** Format: date-time */
      expiresAt?: string;
      /** Format: date-time */
      respondedAt?: string | null;
    };
    TeamInvitationDtoBaseResponse: {
      data: components['schemas']['TeamInvitationDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    TeamInvitationDtoPaginatedResponse: {
      data: components['schemas']['TeamInvitationDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    TeamJoinRequestDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      concurrencyToken?: string;
      /** Format: uuid */
      id?: string;
      /** Format: uuid */
      teamId?: string;
      /** Format: uuid */
      userId?: string;
      message?: string | null;
      status?: components['schemas']['TeamRequestStatus'];
      /** Format: uuid */
      reviewedBy?: string | null;
      /** Format: date-time */
      respondedAt?: string | null;
    };
    TeamJoinRequestDtoBaseResponse: {
      data: components['schemas']['TeamJoinRequestDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    TeamJoinRequestDtoPaginatedResponse: {
      data: components['schemas']['TeamJoinRequestDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    TeamMemberDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      teamId?: string;
      /** Format: uuid */
      userId?: string;
      role?: components['schemas']['TeamRole'];
      status?: components['schemas']['MemberStatus'];
      /** Format: date-time */
      joinedAt?: string;
      /** Format: date-time */
      leftAt?: string | null;
    };
    TeamMemberDtoPaginatedResponse: {
      data: components['schemas']['TeamMemberDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    /** @enum {string} */
    TeamRequestStatus:
      | 'PENDING'
      | 'ACCEPTED'
      | 'REJECTED'
      | 'WITHDRAWN'
      | 'CANCELLED'
      | 'EXPIRED';
    /** @enum {string} */
    TeamRole: 'CAPTAIN' | 'MEMBER' | 'DEPUTY';
    /** @enum {string} */
    TeamStatus: 'ACTIVE' | 'INACTIVE' | 'ARCHIVED';
    TeamUserRequest: {
      /** Format: uuid */
      userId?: string;
    };
    TeamVersionRequest: {
      /** Format: uuid */
      concurrencyToken?: string;
    };
    TransferTeamCaptainRequest: {
      /** Format: uuid */
      userId?: string;
      /** Format: uuid */
      concurrencyToken?: string;
    };
    UnreadNotificationCountDto: {
      /** Format: int64 */
      count?: number;
    };
    UnreadNotificationCountDtoBaseResponse: {
      data: components['schemas']['UnreadNotificationCountDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    UpdateListingRequest: {
      /** Format: uuid */
      sportId: string;
      /** Format: uuid */
      formatId: string;
      /** Format: uuid */
      hostTeamId?: string | null;
      mode: components['schemas']['ListingMode'];
      title: string;
      description?: string | null;
      note?: string | null;
      levelMin?: components['schemas']['SportLevel'];
      levelMax?: components['schemas']['SportLevel'];
      /** Format: int32 */
      openSlots: number;
      /** Format: date-time */
      startAt: string;
      /** Format: int32 */
      durationMinutes: number;
      /** Format: date-time */
      cutoffAt: string;
      addressText: string;
      /** Format: double */
      latitude: number;
      /** Format: double */
      longitude: number;
      /** Format: uuid */
      concurrencyToken: string;
    };
    UpdateListingWatchRequest: {
      /** Format: uuid */
      sportId: string;
      /** Format: uuid */
      formatId?: string | null;
      /** Format: double */
      latitude: number;
      /** Format: double */
      longitude: number;
      /** Format: int32 */
      radiusMeters: number;
      /** Format: date-time */
      startFrom: string;
      /** Format: date-time */
      startTo: string;
      emailEnabled?: boolean;
      /** Format: uuid */
      concurrencyToken: string;
    };
    UpdateNotificationConfigRequest: {
      description?: string | null;
      isSendNow?: boolean;
      scheduleType: string;
      cronString?: string | null;
      critical: string;
      title: string;
      body: string;
      /** Format: date-time */
      availableAt?: string;
      /** Format: date-time */
      expiredAfter?: string | null;
      /** Format: int32 */
      maxSendTimes?: number | null;
      audienceType: string;
      userIds?: string[] | null;
      teamIds?: string[] | null;
      inAppEnabled?: boolean;
      emailEnabled?: boolean;
      pushEnabled?: boolean;
      smsEnabled?: boolean;
      metadata?: unknown;
      timeZoneId: string;
      /** Format: uuid */
      concurrencyToken?: string;
    };
    UpdateRolePermissionsRequest: {
      permissionCodes: string[];
      concurrencyStamp: string;
    };
    UpdateRoleRequest: {
      name: string;
      description?: string | null;
      isActive: boolean;
      isDefaultForRegistration: boolean;
      concurrencyStamp: string;
    };
    UpdateSportRequest: {
      code: string;
      name: string;
      description?: string | null;
      formats: components['schemas']['SportFormatRequest'][];
    };
    UpdateSystemSettingsRequest: {
      /** Format: int32 */
      outboxMaxRetryCount: number;
      /** Format: int32 */
      maxCaptainTeamsPerUser: number;
      /** Format: int32 */
      maxMemberTeamsPerUser: number;
      /** Format: int32 */
      maxDeputiesPerTeam: number;
      /** Format: int32 */
      maxMembersPerTeam: number;
      /** Format: int32 */
      teamInvitationLifetimeHours: number;
      /** Format: int32 */
      maxPendingTeamRequests: number;
      /** Format: int32 */
      outboxRetryInitialDelaySeconds: number;
      /** Format: int32 */
      outboxRetryMaxDelaySeconds: number;
      /** Format: int32 */
      processedOutboxRetentionDays: number;
      /** Format: int32 */
      notificationRetentionDays: number;
      /** Format: uuid */
      concurrencyToken: string;
      /** Format: int32 */
      maxActiveListingWatchesPerUser: number;
      /** Format: int32 */
      maxListingSearchRadiusMeters: number;
      /** Format: int32 */
      maxListingWatchWindowDays: number;
      /** Format: int32 */
      matchReminderLeadMinutes: number;
    };
    UpdateTeamRequest: {
      name: string;
      description?: string | null;
      homeArea?: string | null;
      avatarUrl?: string | null;
      /** Format: uuid */
      concurrencyToken?: string;
    };
    UpdateUserPreferencesRequest: {
      languageCode: string;
      colorMode: string;
      themePreset: string;
      /** Format: uuid */
      concurrencyToken?: string | null;
    };
    UpdateUserProfileRequest: {
      displayName: string;
      /** Format: uri */
      avatarUrl?: string | null;
      defaultArea?: string | null;
      contactNote?: string | null;
    };
    UpdateUserSportProfileRequest: {
      level: string;
      preferredFormatIds: string[];
      bio?: string | null;
    };
    UserListDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      id?: string;
      displayName?: string | null;
      email?: string | null;
      emailConfirmed?: boolean;
      status?: components['schemas']['UserStatus'];
      /** Format: date-time */
      lastLoginAt?: string | null;
      roles?: components['schemas']['UserListRoleDto'][] | null;
    };
    UserListDtoPaginatedResponse: {
      data: components['schemas']['UserListDto'][] | null;
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
      pagination: components['schemas']['PaginationMetadata'];
    };
    UserListRoleDto: {
      /** Format: uuid */
      id?: string;
      name?: string | null;
      isActive?: boolean;
    };
    UserPreferencesDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      concurrencyToken?: string;
      languageCode?: string | null;
      colorMode?: string | null;
      themePreset?: string | null;
    };
    UserPreferencesDtoBaseResponse: {
      data: components['schemas']['UserPreferencesDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    UserProfileDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      userId?: string;
      displayName?: string | null;
      avatarUrl?: string | null;
      defaultArea?: string | null;
      contactNote?: string | null;
    };
    UserProfileDtoBaseResponse: {
      data: components['schemas']['UserProfileDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    UserRoleDto: {
      /** Format: uuid */
      userId?: string;
      roles?: string[] | null;
      roleIds?: string[] | null;
      concurrencyStamp?: string | null;
    };
    UserRoleDtoBaseResponse: {
      data: components['schemas']['UserRoleDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    UserSportProfileDto: {
      /** Format: date-time */
      createdAt?: string;
      /** Format: date-time */
      updatedAt?: string;
      /** Format: uuid */
      createdBy?: string | null;
      /** Format: uuid */
      updatedBy?: string | null;
      /** Format: uuid */
      id?: string;
      /** Format: uuid */
      sportId?: string;
      sportCode?: string | null;
      sportName?: string | null;
      level?: components['schemas']['SportLevel'];
      bio?: string | null;
      preferredFormats?: components['schemas']['SportFormatDto'][] | null;
    };
    UserSportProfileDtoBaseResponse: {
      data: components['schemas']['UserSportProfileDto'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    /** @enum {string} */
    UserStatus: 'ACTIVE' | 'SUSPENDED' | 'DELETED';
    WebAuthTokenResponse: {
      accessToken?: string | null;
      tokenType?: string | null;
      /** Format: date-time */
      accessTokenExpiresAt?: string;
      user?: components['schemas']['CurrentUserResponse'];
    };
    WebAuthTokenResponseBaseResponse: {
      data: components['schemas']['WebAuthTokenResponse'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
    WebCsrfResponse: {
      requestToken?: string | null;
    };
    WebCsrfResponseBaseResponse: {
      data: components['schemas']['WebCsrfResponse'];
      message?: string | null;
      traceId: string | null;
      correlationId: string | null;
    };
  };
  responses: never;
  parameters: never;
  requestBodies: never;
  headers: never;
  pathItems: never;
}
export type $defs = Record<string, never>;
export type operations = Record<string, never>;
