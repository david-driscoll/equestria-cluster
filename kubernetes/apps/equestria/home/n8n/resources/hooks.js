const { dirname, resolve } = require("path");
const Layer = require("router/lib/layer");
const { issueCookie } = require(resolve(
  dirname(require.resolve("n8n")),
  "auth/jwt"
));
const ignoreAuthRegexp = /^\/(assets|healthz|webhook|rest\/oauth2-credential)/;
module.exports = {
  n8n: {
    ready: [
      async function ({ app }, config) {
        const { stack } = app.router;
        const index = stack.findIndex((l) => l.name === "cookieParser");
        stack.splice(
          index + 1,
          0,
          new Layer(
            "/",
            {
              strict: false,
              end: false,
            },
            async (req, res, next) => {
              console.log("Forward Auth Middleware");
              // skip if URL is ignored
              if (ignoreAuthRegexp.test(req.url)) {
                console.log(`Skipping auth for ${req.url}`);
                return next();
              }

              // skip if user management is not set up yet
              if (!config.get("userManagement.isInstanceOwnerSetUp", false)) {
                console.log("Skipping auth: user management is not set up");
                return next();
              }

              // skip if cookie already exists
              if (req.cookies?.["n8n-auth"]) {
                console.log("Skipping auth: cookie already exists");
                return next();
              }

              // if N8N_FORWARD_AUTH_HEADER is not set, skip
              if (!process.env.N8N_FORWARD_AUTH_HEADER) {
                console.log("Skipping auth: N8N_FORWARD_AUTH_HEADER is not set");
                return next();
              }

              // if N8N_FORWARD_AUTH_HEADER header is not found, skip
              const email =
                req.headers[process.env.N8N_FORWARD_AUTH_HEADER.toLowerCase()];
              if (!email) {
                console.log("Skipping auth: N8N_FORWARD_AUTH_HEADER header is not found");
                console.log(`headers: ${JSON.stringify(Object.keys(req.headers))}`);
                return next();
              }

              // search for user with email
              const user = await this.dbCollections.User.findOneBy({ email });
              if (!user) {
                console.log(`User ${email} not found`);
                res.statusCode = 401;
                res.end(
                  `User ${email} not found, please have an admin invite the user first.`
                );
                return;
              }

               console.log(`Authenticated as ${email}`);
              // issue cookie if all is OK
              issueCookie(res, user);
              return next();
            }
          )
        );
      },
    ],
  },
};
