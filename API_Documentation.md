# EGC Ticketing System - API Documentation

Welcome to the backend API documentation for the EGC Ticketing System. This document serves as a complete reference for frontend developers integration.

---

## Table of Contents
1. [General Specifications](#1-general-specifications)
2. [Enums Reference](#2-enums-reference)
3. [Authentication API (`api/auth`)](#3-authentication-api-apiauth)
4. [Profile API (`api/profile`)](#4-profile-api-apiprofile)
5. [User Management API (`api/users`)](#5-user-management-api-apiusers)
6. [Team Management API (`api/teams`)](#6-team-management-api-apiteams)
7. [Ticket & Task Management API (`api/tickets`)](#7-ticket--task-management-api-apitickets)
8. [Performance Ratings API (`api/userrates`)](#8-performance-ratings-api-apiuserrates)
9. [Analysis & Dashboard API (`api/analysis`)](#9-analysis--dashboard-api-apianalysis)
10. [Audit Logs API (`api/logs`)](#10-audit-logs-api-apilogs)

---

## 1. General Specifications

### Base URL
All API paths listed in this document are relative to the root URL (e.g., `https://api.yourdomain.com/api` or `http://localhost:5000/api`).
* **Base Path Format:** `/api/[controller]`

### Authentication
Most endpoints (except login, forgot password, and reset password) require JWT Authentication.
* **Header Format:** `Authorization: Bearer <JWT_TOKEN>`
* **Token Payload Claims:**
  * `NameIdentifier`: User ID (integer)
  * `Role`: User Role (`Admin`, `Manager`, `Member`)

### Error Responses
Standard HTTP status codes are used to indicate request outcomes:
* `200 OK` / `201 Created`: Request succeeded.
* `400 Bad Request`: Model validation failed, or business rules were violated. Returns `ModelState` errors or a message JSON: `{ "message": "error details" }`.
* `401 Unauthorized`: Token is invalid or expired.
* `403 Forbidden`: Authenticated user lacks roles/permissions to perform the action.
* `404 Not Found`: The requested entity does not exist or has been soft-deleted.

---

## 2. Enums Reference

These enums are serialized as integer index values (0-based) or strings depending on serializer configuration. Generally, the API processes them as their corresponding names (strings) or integer values.

### `UserRole`
* `0` / `Admin`
* `1` / `Manager`
* `2` / `Member`

### `UserStatus`
* `0` / `Active`
* `1` / `Blocked`
* `2` / `Deleted` (Used internally for soft deletes)

### `TeamStatus`
* `0` / `Active`
* `1` / `Pending`
* `2` / `Finished`
* `3` / `Deleted`

### `TicketStatus`
* `0` / `NotAssigned`
* `1` / `Pending`
* `2` / `OnProgress`
* `3` / `Completed`
* `4` / `Deleted`

### `TicketPriority` & `TicketTaskPriority`
* `0` / `Low`
* `1` / `Medium`
* `2` / `High`
* `3` / `Urgent`

### `TicketTaskStatus`
* `0` / `NotAssigned`
* `1` / `Pending`
* `2` / `OnProgress`
* `3` / `Completed`
* `4` / `Approved`
* `5` / `Deleted`

### `UserRateType`
* `0` / `Standard` (Auto-approved ratings)
* `1` / `Report` (Requires manager/admin approval)

---

## 3. Authentication API (`api/auth`)

These endpoints manage user sessions and credentials.

### Login
* **URL:** `/api/auth/login`
* **Method:** `POST`
* **Auth Required:** No
* **Request Body (JSON):**
  ```json
  {
    "identifier": "username_email_or_phone",
    "password": "yourpassword"
  }
  ```
* **Response `200 OK`:**
  ```json
  {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "userId": 12,
    "username": "johndoe",
    "role": "Manager"
  }
  ```
* **Response `401 Unauthorized`:** Invalid credentials or account deleted.
* **Response `403 Forbidden`:** Account is blocked.

### Forgot Password
* **URL:** `/api/auth/forgot-password`
* **Method:** `POST`
* **Auth Required:** No
* **Request Body (JSON):**
  ```json
  {
    "email": "user@example.com"
  }
  ```
* **Response `200 OK`:** 
  ```json
  {
    "message": "If the email exists, an OTP has been sent."
  }
  ```
  *(Sends a 6-digit OTP code valid for 10 minutes to the user's email.)*

### Reset Password
* **URL:** `/api/auth/reset-password`
* **Method:** `POST`
* **Auth Required:** No
* **Request Body (JSON):**
  ```json
  {
    "email": "user@example.com",
    "otp": "123456",
    "newPassword": "newsecurepassword123"
  }
  ```
* **Response `200 OK`:**
  ```json
  {
    "message": "Password reset successfully."
  }
  ```

### Logout
* **URL:** `/api/auth/logout`
* **Method:** `POST`
* **Auth Required:** Optional (Gracefully degrades if token expired)
* **Response `200 OK`:**
  ```json
  {
    "message": "Logged out successfully. Please discard the JWT token on client."
  }
  ```

---

## 4. Profile API (`api/profile`)

Endpoints for the currently logged-in user to manage their own profile details.

### Get Profile Details
* **URL:** `/api/profile`
* **Method:** `GET`
* **Auth Required:** Yes
* **Response `200 OK`:**
  ```json
  {
    "id": 12,
    "fullName": "John Doe",
    "username": "johndoe",
    "email": "johndoe@example.com",
    "phoneNumber": "+123456789",
    "jobTitle": "Lead Developer",
    "role": "Manager",
    "status": "Active",
    "createdAt": "2026-06-30T10:00:00Z",
    "signatureUrl": "/uploads/signatures/uuid_filename.png",
    "createdById": 1
  }
  ```

### Update Profile Details
> [!IMPORTANT]
> This endpoint uses `multipart/form-data` to accommodate image signature uploads.
* **URL:** `/api/profile`
* **Method:** `PUT`
* **Auth Required:** Yes
* **Content-Type:** `multipart/form-data`
* **Form Fields:**
  * `FullName` (Required, string)
  * `Email` (Optional, string - email format)
  * `PhoneNumber` (Optional, string)
  * `JobTitle` (Optional, string)
  * `Signature` (Optional, File - Image file containing signature)
* **Response `200 OK`:**
  ```json
  {
    "message": "Profile updated successfully."
  }
  ```

### Change Password
* **URL:** `/api/profile/change-password`
* **Method:** `PUT`
* **Auth Required:** Yes
* **Request Body (JSON):**
  ```json
  {
    "currentPassword": "oldpassword123",
    "newPassword": "newpassword123"
  }
  ```
* **Response `200 OK`:**
  ```json
  {
    "message": "Password updated successfully."
  }
  ```

---

## 5. User Management API (`api/users`)

This endpoint is restricted to **Admin** and **Manager** roles for managing directory users.

### Get All Users
* **URL:** `/api/users`
* **Method:** `GET`
* **Auth Required:** Yes (Admin, Manager)
* **Response `200 OK`:** Array of user response objects.
  ```json
  [
    {
      "id": 1,
      "fullName": "Jane Admin",
      "username": "admin",
      "email": "admin@example.com",
      "phoneNumber": "+123",
      "jobTitle": "System Admin",
      "role": "Admin",
      "status": "Active",
      "createdAt": "2026-06-30T10:00:00Z",
      "signatureUrl": null,
      "createdById": null
    }
  ]
  ```

### Get User by ID
* **URL:** `/api/users/{id}`
* **Method:** `GET`
* **Auth Required:** Yes (Admin, Manager)
* **Response `200 OK`:** Single user object.

### Create User
> [!IMPORTANT]
> This endpoint uses `multipart/form-data` and has role-based constraints. 
> * **Managers** can only create users with the role **Member** (`2`).
* **URL:** `/api/users`
* **Method:** `POST`
* **Auth Required:** Yes (Admin, Manager)
* **Content-Type:** `multipart/form-data`
* **Form Fields:**
  * `FullName` (Required, string)
  * `Username` (Required, string)
  * `Email` (Optional, string - email format)
  * `PhoneNumber` (Optional, string)
  * `Password` (Required, string - min length 6)
  * `JobTitle` (Optional, string)
  * `Role` (Required, integer/enum: `0` = Admin, `1` = Manager, `2` = Member)
  * `Signature` (Optional, File - Image file)
* **Response `201 Created`:** Created user details response object.

### Update User Details
> [!WARNING]
> In the backend controller, this endpoint expects `[FromBody] UpdateUserDto`. Because of this, uploading files directly via multipart form data to this endpoint is not natively supported. Keep `Signature` null or perform edits without signature updates.
> * **Managers** can only edit users who are **Members** and cannot change their roles to **Admin** or **Manager**.
* **URL:** `/api/users/{id}`
* **Method:** `PUT`
* **Auth Required:** Yes (Admin, Manager)
* **Request Body (JSON):**
  ```json
  {
    "fullName": "John Updated",
    "email": "newemail@example.com",
    "phoneNumber": "+987654321",
    "jobTitle": "Senior Analyst",
    "role": 2,
    "status": 0
  }
  ```
* **Response `200 OK`:**
  ```json
  {
    "message": "User updated successfully."
  }
  ```

### Delete User
* **URL:** `/api/users/{id}`
* **Method:** `DELETE`
* **Auth Required:** Yes (Admin, Manager)
* **Response `200 OK`:**
  ```json
  {
    "message": "User deleted successfully."
  }
  ```
  *(Performs a soft delete on the user, clears out team memberships, and sets status to Deleted).*

---

## 6. Team Management API (`api/teams`)

This API manages organizational groups and is restricted to **Admin** and **Manager** roles.

### Get All Teams
* **URL:** `/api/teams`
* **Method:** `GET`
* **Auth Required:** Yes (Admin, Manager)
* **Response `200 OK`:**
  ```json
  [
    {
      "id": 1,
      "name": "Design Team",
      "description": "Handles styling and graphic design assets.",
      "status": "Active",
      "createdAt": "2026-06-30T10:00:00Z",
      "createdById": 2,
      "members": [
        {
          "memberId": 5,
          "fullName": "Jane Artist",
          "username": "janeart",
          "email": "jane@example.com",
          "isTeamLeader": true,
          "joinedAt": "2026-06-30T11:00:00Z"
        }
      ]
    }
  ]
  ```

### Get Team by ID
* **URL:** `/api/teams/{id}`
* **Method:** `GET`
* **Auth Required:** Yes (Admin, Manager)
* **Response `200 OK`:** Single team details object.

### Create Team
* **URL:** `/api/teams`
* **Method:** `POST`
* **Auth Required:** Yes (Admin, Manager)
* **Request Body (JSON):**
  ```json
  {
    "name": "QA Engineers",
    "description": "Testing and validation",
    "status": 0
  }
  ```
* **Response `201 Created`:** Created team details response object.

### Update Team
* **URL:** `/api/teams/{id}`
* **Method:** `PUT`
* **Auth Required:** Yes (Admin, Manager)
* **Request Body (JSON):**
  ```json
  {
    "name": "QA Engineers (Updated)",
    "description": "Testing and validation systems",
    "status": 0
  }
  ```
* **Response `200 OK`:**
  ```json
  {
    "message": "Team updated successfully."
  }
  ```

### Soft Delete Team
* **URL:** `/api/teams/{id}`
* **Method:** `DELETE`
* **Auth Required:** Yes (Admin, Manager)
* **Response `200 OK`:**
  ```json
  {
    "message": "Team deleted successfully."
  }
  ```

### Add Member to Team
* **URL:** `/api/teams/{teamId}/members`
* **Method:** `POST`
* **Auth Required:** Yes (Admin, Manager)
* **Request Body (JSON):**
  ```json
  {
    "memberId": 14
  }
  ```
* **Response `200 OK`:**
  ```json
  {
    "message": "Member added to team successfully."
  }
  ```

### Remove Member from Team
* **URL:** `/api/teams/{teamId}/members/{memberId}`
* **Method:** `DELETE`
* **Auth Required:** Yes (Admin, Manager)
* **Response `200 OK`:**
  ```json
  {
    "message": "Member removed from team successfully."
  }
  ```

### Set Team Leader
* **URL:** `/api/teams/{teamId}/leader`
* **Method:** `POST`
* **Auth Required:** Yes (Admin, Manager)
* **Request Body (JSON):**
  ```json
  {
    "leaderId": 14
  }
  ```
* **Response `200 OK`:**
  ```json
  {
    "message": "Team leader updated successfully."
  }
  ```
  *(Assigns the specified member as leader and removes leadership status from all other members of the team).*

---

## 7. Ticket & Task Management API (`api/tickets`)

Main workflow APIs. Accessible to members, managers, and admins. 
* **Members** only see tickets from the teams they belong to.

### Get All Tickets
* **URL:** `/api/tickets`
* **Method:** `GET`
* **Auth Required:** Yes
* **Response `200 OK`:**
  ```json
  [
    {
      "id": 101,
      "teamId": 1,
      "teamName": "Design Team",
      "memberId": 5,
      "memberName": "Jane Artist",
      "createdById": 2,
      "createdByName": "John Doe",
      "title": "Redesign Landing Page",
      "description": "Revamp CSS styling and assets.",
      "deadline": "2026-07-15T18:00:00Z",
      "createdAt": "2026-06-30T10:00:00Z",
      "completedAt": null,
      "status": "Pending",
      "priority": "High",
      "ticketTasks": [],
      "statusHistories": []
    }
  ]
  ```

### Get Ticket by ID
* **URL:** `/api/tickets/{id}`
* **Method:** `GET`
* **Auth Required:** Yes
* **Response `200 OK`:** Single ticket object. Returns `403 Forbidden` if a Member does not belong to the ticket's team.

### Create Ticket
> [!IMPORTANT]
> Restricted to Admin, Manager, or the Team Leader of the specified team.
* **URL:** `/api/tickets`
* **Method:** `POST`
* **Auth Required:** Yes
* **Request Body (JSON):**
  ```json
  {
    "teamId": 1,
    "memberId": 5,
    "title": "New Ticket Title",
    "description": "Detailed description of tasks",
    "deadline": "2026-07-20T12:00:00Z",
    "priority": 2
  }
  ```
* **Response `201 Created`:** Created ticket object.
  *(Note: Auto-sets status to `Pending` if assigned, or `NotAssigned` if memberId is null).*

### Update Ticket
> [!IMPORTANT]
> Restricted to Admin, Manager, or Team Leader of the ticket's team.
* **URL:** `/api/tickets/{id}`
* **Method:** `PUT`
* **Auth Required:** Yes
* **Request Body (JSON):**
  ```json
  {
    "memberId": 5,
    "title": "Updated Title",
    "description": "Updated Description",
    "deadline": "2026-07-20T12:00:00Z",
    "status": 2,
    "priority": 3
  }
  ```
* **Response `200 OK`:**
  ```json
  {
    "message": "Ticket updated successfully."
  }
  ```

### Soft Delete Ticket
> [!IMPORTANT]
> Restricted to Admin, Manager, or Team Leader of the ticket's team.
* **URL:** `/api/tickets/{id}`
* **Method:** `DELETE`
* **Auth Required:** Yes
* **Response `200 OK`:**
  ```json
  {
    "message": "Ticket deleted successfully."
  }
  ```

### Change Ticket Status
> [!IMPORTANT]
> This endpoint uses `multipart/form-data` and has a file size cap of **5MB** for attachment.
> * **Allowed Users:** Admin, Manager, Assigned Member, or Team Leader of the team.
> * **AddAnotherTask option:** If set to `true`, a follow-up task is auto-created with the provided values.
* **URL:** `/api/tickets/{id}/status`
* **Method:** `PUT`
* **Auth Required:** Yes
* **Content-Type:** `multipart/form-data`
* **Form Fields:**
  * `Status` (Required, integer/enum: `0`=NotAssigned, `1`=Pending, `2`=OnProgress, `3`=Completed)
  * `Comment` (Optional, string - max length 1000)
  * `LinkUrl` (Optional, string - max length 500)
  * `File` (Optional, File - Max 5MB attachment)
  * `AddAnotherTask` (Optional, boolean)
  * `NewTaskTitle` (Optional, string)
  * `NewTaskDescription` (Optional, string)
  * `NewTaskDeadline` (Optional, string - Date format)
  * `NewTaskMemberId` (Optional, integer)
  * `NewTaskPriority` (Optional, integer/enum: `0`=Low, `1`=Medium, `2`=High, `3`=Urgent)
* **Response `200 OK`:**
  ```json
  {
    "message": "Ticket status updated successfully."
  }
  ```

### Get All Tasks for a Ticket
* **URL:** `/api/tickets/{ticketId}/tasks`
* **Method:** `GET`
* **Auth Required:** Yes
* **Response `200 OK`:** List of `TicketTaskResponseDto` objects.

### Get Task by ID
* **URL:** `/api/tickets/{ticketId}/tasks/{taskId}`
* **Method:** `GET`
* **Auth Required:** Yes
* **Response `200 OK`:** Single `TicketTaskResponseDto` object.

### Create Task for Ticket
> [!IMPORTANT]
> Restricted to Admin, Manager, or Team Leader of the team.
* **URL:** `/api/tickets/{ticketId}/tasks`
* **Method:** `POST`
* **Auth Required:** Yes
* **Request Body (JSON):**
  ```json
  {
    "title": "Subtask Title",
    "description": "Details",
    "deadline": "2026-07-15T00:00:00Z",
    "memberId": 5,
    "priority": 1
  }
  ```
* **Response `201 Created`:** Created task details.

### Update Task details
> [!IMPORTANT]
> Restricted to Admin, Manager, or Team Leader of the team.
* **URL:** `/api/tickets/{ticketId}/tasks/{taskId}`
* **Method:** `PUT`
* **Auth Required:** Yes
* **Request Body (JSON):**
  ```json
  {
    "title": "Updated Subtask Title",
    "description": "Updated Details",
    "deadline": "2026-07-15T00:00:00Z",
    "memberId": 5,
    "status": 2,
    "priority": 2
  }
  ```
* **Response `200 OK`:**
  ```json
  {
    "message": "Task updated successfully."
  }
  ```

### Change Task Status
> [!IMPORTANT]
> This endpoint uses `multipart/form-data` and has a file size cap of **5MB** for attachment.
> * **Allowed Users:** Admin, Manager, Assigned Member, or Team Leader of the team.
* **URL:** `/api/tickets/{ticketId}/tasks/{taskId}/status`
* **Method:** `PUT`
* **Auth Required:** Yes
* **Content-Type:** `multipart/form-data`
* **Form Fields:**
  * `Status` (Required, integer/enum: `0`=NotAssigned, `1`=Pending, `2`=OnProgress, `3`=Completed, `4`=Approved)
  * `Comment` (Optional, string)
  * `LinkUrl` (Optional, string)
  * `File` (Optional, File - Max 5MB attachment)
  * `AddAnotherTask` (Optional, boolean)
  * `NewTaskTitle` (Optional, string)
  * `NewTaskDescription` (Optional, string)
  * `NewTaskDeadline` (Optional, string)
  * `NewTaskMemberId` (Optional, integer)
  * `NewTaskPriority` (Optional, integer)
* **Response `200 OK`:**
  ```json
  {
    "message": "Task status updated successfully."
  }
  ```

---

## 8. Performance Ratings API (`api/userrates`)

Enables team members to evaluate and grade each other. 
* **Approval rules:**
  * Ratings submitted by **Admins** are automatically approved (`IsApproved: true`).
  * Ratings submitted by **Managers** on **Admins** require approval (`IsApproved: false`, type: `Report`).
  * Ratings submitted by **Managers** on anyone else are auto-approved.
  * Ratings submitted by **Team Leaders** on Admins/Managers require approval (`IsApproved: false`, type: `Report`).
  * Ratings submitted by regular **Members** on anyone require approval (`IsApproved: false`, type: `Report`).

### Create User Rating
* **URL:** `/api/userrates`
* **Method:** `POST`
* **Auth Required:** Yes
* **Request Body (JSON):**
  ```json
  {
    "toUserId": 14,
    "comment": "Exceptional support on ticket redesign",
    "rateItems": [
      {
        "title": "Speed",
        "value": 9.5,
        "maxValue": 10
      },
      {
        "title": "Code Quality",
        "value": 8.0,
        "maxValue": 10
      }
    ]
  }
  ```
* **Response `201 Created`:** UserRateResponseDto details.

### Get Rating by ID
* **URL:** `/api/userrates/{id}`
* **Method:** `GET`
* **Auth Required:** Yes
* **Response `200 OK`:** Single rating response. 
  *(Checks permissions: Admins see all. Managers/Leaders see members of their teams. Evaluators and evaluees see their own).*

### Get All User Ratings
* **URL:** `/api/userrates`
* **Method:** `GET`
* **Auth Required:** Yes
* **Response `200 OK`:** Returns list of visible ratings.
  * **Admins** get all ratings.
  * **Managers** see ratings they submitted, ratings they received, and ratings received by members in teams they created.
  * **Team Leaders** see ratings they submitted, ratings they received, and ratings received by members in teams they lead.
  * **Members** see only ratings they submitted or received.

### Get Pending Ratings
* **URL:** `/api/userrates/pending`
* **Method:** `GET`
* **Auth Required:** Yes
* **Response `200 OK`:** Returns list of pending ratings awaiting approval.
  * **Admins** see all pending ratings.
  * **Managers** see pending ratings submitted by members in teams they manage.
  * **Team Leaders** see pending ratings submitted by members in teams they lead.

### Approve/Reject Rating
* **URL:** `/api/userrates/{id}/approve`
* **Method:** `PUT`
* **Auth Required:** Yes
* **Request Body (JSON):**
  ```json
  {
    "approve": true,
    "approvalComment": "Matches internal performance metrics."
  }
  ```
* **Response `200 OK`:**
  ```json
  {
    "message": "Rating approved successfully." 
  }
  ```
  *(If `approve: false`, the rating is deleted and the message returns: `"Rating rejected and deleted successfully."`)*

---

## 9. Analysis & Dashboard API (`api/analysis`)

Dashboard aggregation endpoint for calculating performance stats and metrics.

### Get Analysis Stats
* **URL:** `/api/analysis`
* **Method:** `GET`
* **Auth Required:** Yes
* **Query Parameters:**
  * `teamId` (Optional, integer) -> Filter dashboard by team ID.
  * `managerId` (Optional, integer) -> Filter dashboard by manager ID.
* **Response `200 OK`:**
  ```json
  {
    "totalTeams": 3,
    "totalTickets": 24,
    "ticketsNotAssigned": 5,
    "ticketsPending": 10,
    "ticketsOnProgress": 6,
    "ticketsCompleted": 3,
    "teamSummaries": [
      {
        "teamId": 1,
        "teamName": "Design Team",
        "totalTickets": 12,
        "completedTickets": 2
      }
    ],
    "highestRatedMember": {
      "userId": 5,
      "fullName": "Jane Artist",
      "averageRate": 8.75
    },
    "highestRatedLeader": {
      "userId": 6,
      "fullName": "Adam Leader",
      "averageRate": 9.2
    },
    "highestRatedManager": null,
    "lowestRatedMember": null,
    "lowestRatedLeader": null,
    "lowestRatedManager": null,
    "mostTasksDoneMember": {
      "userId": 5,
      "fullName": "Jane Artist",
      "tasksDoneCount": 18
    }
  }
  ```

---

## 10. Audit Logs API (`api/logs`)

System audits for compliance logging. Restricted to **Admin** role only.

### Get Logs (Paginated & Filtered)
* **URL:** `/api/logs`
* **Method:** `GET`
* **Auth Required:** Yes (Admin only)
* **Query Parameters:**
  * `userId` (Optional, integer)
  * `action` (Optional, string)
  * `entityName` (Optional, string)
  * `entityId` (Optional, string)
  * `startDate` (Optional, DateTime string)
  * `endDate` (Optional, DateTime string)
  * `skip` (Optional, integer, default `0`)
  * `limit` (Optional, integer, default `50`, max capped at `100`)
* **Response `200 OK`:**
  ```json
  {
    "totalCount": 150,
    "logs": [
      {
        "id": 431,
        "userId": 12,
        "userFullName": "John Doe",
        "action": "Login",
        "entityName": "User",
        "entityId": "12",
        "createdAt": "2026-06-30T12:15:00Z"
      }
    ]
  }
  ```

### Get Log Details by ID
* **URL:** `/api/logs/{id}`
* **Method:** `GET`
* **Auth Required:** Yes (Admin only)
* **Response `200 OK`:** Detailed log entry description.
  ```json
  {
    "id": 431,
    "userId": 12,
    "username": "johndoe",
    "userFullName": "John Doe",
    "action": "Login",
    "entityName": "User",
    "entityId": "12",
    "details": "User johndoe successfully logged in.",
    "createdAt": "2026-06-30T12:15:00Z"
  }
  ```
