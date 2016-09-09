# Tasks API #

This API provides a mechanism for displaying tasks to groups of users and a means of recording which users have dismissed and/or completed those tasks. 


## Terms ##

**Party/assignee** = an arbitrary identifier for a group of users to which tasks can be assigned

**User** = an individual that can retrieve tasks assigned to a party and can complete any of those tasks

**Task** = an activity that needs to be performed by a party. Tasks are based on task templates

**Task template** = denotes a type of task, eg. "confirm apprenticeship"

**Task alert** = a record that a user has viewed / dismissed a specific task

Note that user identifiers and parties are stored as strings are are not validated. This allows the API consumer to determine their own formats, eg. a party may be an Employer Account ID or a UKPRN. 


## Functionality ##

### Creating tasks ###

To create a new task for a party:

POST http://host:port/api/tasks/{assignee}

	{
	  "taskTemplateId": 1,
      "body": "some payload"
	}

Where:

- **assignee** refers to a party (eg. `10123456` or `Sainsburys`)
- **taskTemplate** refers to an existing task template
- **body** is an optional payload (eg. message or serialised data)

### Retrieving tasks for a party (assignee) ###

To retrieve all tasks assigned to a party:

GET http://host:port/api/tasks/{assignee}

Where:

- **assignee** refers to a party (eg. `10123456` or `Sainsburys`)

### Creating task alerts ###

To record a new task alert for a particular user:

POST http://host:port/api/taskalerts/{userId}

	{
	  "taskId": 3
	}

Where:

- **userId** refers to a user (eg. `John` or `john@example.com`)
- **taskId** refers to the task to which the alert relates

### Retrieving a user's task alerts ###

To retrieve a list of alerts for a particular user:

GET http://host:port/api/taskalerts/{userId}

Where:

- **userId** refers to a user (eg. `John` or `john@example.com`)

### Completing a task ###

To mark a task as completed:

PUT http://host:port/api/tasks/{taskId}
	
	{
	  "completedBy": "Peter"
	}

Where:

- **taskId** refers to a previously created task (eg. 123)
- **completedBy** refers to the user that completed the task (can be any value)


## Examples ##

A few examples of how tasks can be used:

- When an employer creates a commitment and "submits" it to the provider, a new task is created for the provider. The assignee is the provider (eg. their UKPRN) and the task is based on template "review commitment", for example. 

- When a provider user signs in, they are informed of any open tasks relating to their UKPRN. This is derived from the list of tasks for the provider organisation (ie. the UKPRN denotes the party) to which they belong (ie. from their claims). 

- When a provider has added apprenticeship details to a commitment, they can "submit" it to the employer for approval. The assignee is the employer (eg. employer account identifier) and the task template is "approve apprenticeships", for example. 

Note that task completion is defined by the consuming application. Therefore it is up to the application to call the API when a task is considered complete. 


## Security ##

TODO