Should we keep Id (Tenant, Partner etc) as guid or int?

- User - remove and use Membershipprovider roles
- Identifier should be unique - think thread safety (guid for now?)
- Tenant follow DAL model - e.g. get all ftp locations
- ClaimAckServiceConfiguration - how E2E will work? Mesage has id so how do we select provide
- Do we need to store assembly in DocumentProcessorServiceConfiguration

- Invitation - This should be stored in azure table not in blob
- Should identifier have Tenant name which can serve as container name?