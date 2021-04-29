using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Game4
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;

        SpriteBatch spriteBatch;
        BasicEffect effectText;
        BasicEffect effect;
        BasicEffect shadowEffect;
        BasicEffect hintEffect;

        SpriteFont font;
        SpriteFont font1;

        Song ballhits;
        Song songGameOver;
        


        public struct Rectangle
        {
            public Vector3 position;

            public Rectangle(Vector3 pos)
            {
                position = pos;
            }
        }

        public struct Ball
        {
            public Vector3 position;
            public Vector3 velocity;

            public Ball(Vector3 pos, Vector3 vel)
            {
                position = pos;
                velocity = vel;
            }
        }

        Matrix world;
        Matrix view;
        Matrix projection;

        Texture2D skyBox;
        VertexBuffer skyBoxVertexBuffer;
        Texture2D field;
        VertexBuffer fieldBuffer;
        Texture2D shadow;

        private Model cubeModel;
        private Model sphereModel;
        private Effect paddleEffect;

        public Rectangle[] paddles = new Rectangle[2];
        public Ball sphere = new Ball();

        private Vector3 paddleDim = new Vector3(2f, 2f, 0.2f);

        Vector3 cameraPosition = new Vector3(50f, 0f, 0f);
        Vector3 cameraTarget = new Vector3(0f, 0f, 0f);
        Vector3 cameraUp = new Vector3(0f, 1f, 0f);


        private const float ONE_THIRD = 1/3f;
        private const float TWO_THIRD = 2/3f;
        private const float ONE_FOURTH = 0.25f;
        private const float ONE_HALF = 0.5f;
        private const float THREE_FOURTH = 0.75f;
        private const float ANGLE = 0.03f;
        private const float MOVE_SCALE = 0.25f;
        private const float COMP_SPEED = 0.03f;
        private const float BALL_RADIUS = 2;
        private const float BORDER_PADDLE = 20;
        private const float BALL_SPEED = 10;
        private const float SHADOW_ALIGN = 3 + 1 / 3f;
        private const float TWOPLAYER_HINT = 25;
        private const float ONEPLAYER_HINT = 30;

        private readonly Vector3 ORIGIN= new Vector3(0f, 0f, 0f);
        private readonly Vector3 BORDER = new Vector3(20, 20, 40);
        private readonly Vector3 PADDLE0_ORIGIN = new Vector3(0, 0, 40);
        private readonly Vector3 PADDLE1_ORIGIN = new Vector3(0, 0, -40);

        float hintAlpha = 0f;

        float axisYRotate = 0f;

        bool gameOver = false;
        bool twoPlayer = false;
        bool playerTurn = false;
        bool coordsFound = false;
        bool positioned = false;
        bool gameoverSound = false;

        int playerPoints = 0;
        int enemyPoints = 0;

        // Random Start direction
        public float randomStart(Random rand)
        {
            double plusmin = rand.NextDouble();
            if (plusmin > 0.5f)
                return BALL_SPEED;
            else
                return -BALL_SPEED;
        }

        // Calculates angle of the ball off of paddle
        public Vector3 bounce(Vector3 ballPos, Vector3 paddlePos, Vector3 ballVel)
        {
            float xDiff = ballPos.X - paddlePos.X;
            float yDiff = ballPos.Y - paddlePos.Y;
            ballVel.Normalize();
            ballVel.X += xDiff;
            ballVel.Y += yDiff;
            ballVel.Normalize();
            ballVel *= BALL_SPEED;
            return ballVel;
        }

        // Finds the coordinates of where the ball will hit +- BORDER.Z
        public Vector3 findGoal(Ball ball)
        {
            float time;
            Vector3 finalPos;
            if (ball.velocity.Z < 0)
            {
                time = ((-BORDER.Z + BALL_RADIUS) - ball.position.Z) / ball.velocity.Z;
                finalPos = ball.position + ball.velocity * time;
                if (inBox(finalPos))
                    return finalPos;
            }
            else
            {
                time = ((BORDER.Z - BALL_RADIUS) - ball.position.Z) / ball.velocity.Z;
                finalPos = ball.position + ball.velocity * time;
                if (inBox(finalPos))
                    return finalPos;
            }

            if (ball.velocity.X > 0)
            {
                time = ((BORDER.X - BALL_RADIUS) - ball.position.X) / ball.velocity.X;
                finalPos = ball.position + ball.velocity * time;
                if (inBox(finalPos))
                {
                    ball.velocity.X *= -1;
                    ball.position = finalPos;
                    return findGoal(ball);
                }
            }
            else if (ball.velocity.X < 0)
            {
                time = ((-BORDER.X + BALL_RADIUS) - ball.position.X) / ball.velocity.X;
                finalPos = ball.position + ball.velocity * time;
                if (inBox(finalPos))
                {
                    ball.velocity.X *= -1;
                    ball.position = finalPos;
                    return findGoal(ball);
                }
            }

            if (ball.velocity.Y > 0)
            {
                time = ((BORDER.Y - BALL_RADIUS) - ball.position.Y) / ball.velocity.Y;
                finalPos = ball.position + ball.velocity * time;
                if (inBox(finalPos))
                {
                    ball.velocity.Y *= -1;
                    ball.position = finalPos;
                    return findGoal(ball);
                }
            }
            else if (ball.velocity.Y < 0)
            {
                time = ((-BORDER.Y + BALL_RADIUS) - ball.position.Y) / ball.velocity.Y;
                finalPos = ball.position + ball.velocity * time;
                if (inBox(finalPos))
                {
                    ball.velocity.Y *= -1;
                    ball.position = finalPos;
                    return findGoal(ball);
                }
            }
            return ball.position;
        }

        // Checks if ball trajectory is in the box
        // Sub-method to findGoal
        public bool inBox(Vector3 position)
        {
            return (position.X <= BORDER.X - BALL_RADIUS && position.X >= -BORDER.X + BALL_RADIUS && position.Y <= BORDER.Y - BALL_RADIUS && position.Y >= -BORDER.Y + BALL_RADIUS);
        }
        
        // Moves enemy paddle to terget location
        public Vector3 moveAI(Vector3 paddle_pos, Vector3 target)
        {
            if (paddle_pos.X == target.X && paddle_pos.Y == target.Y)
            {
                positioned = true;
                return paddle_pos;
            }
            else
            {
                if (paddle_pos.X > target.X)
                    paddle_pos.X -= COMP_SPEED;
                if (paddle_pos.X< target.X)
                    paddle_pos.X += COMP_SPEED;
                if (paddle_pos.Y > target.Y)
                        paddle_pos.Y -= COMP_SPEED;
                if (paddle_pos.Y < target.Y)
                    paddle_pos.Y += COMP_SPEED;
                return paddle_pos;
            }
        }

        

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";
            // FULL SCREEN NEEDS TO BE IMPROVED
            //graphics.IsFullScreen = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            view = Matrix.CreateLookAt(cameraPosition, cameraTarget, cameraUp);
            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), 800f / 480f, 1f, 1000f);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            effect = new BasicEffect(GraphicsDevice);
            shadowEffect = new BasicEffect(GraphicsDevice);
            hintEffect = new BasicEffect(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            cubeModel = Content.Load<Model>("cube");
            sphereModel = Content.Load<Model>("sphere");
            skyBox = Content.Load<Texture2D>("map");
            field = Content.Load<Texture2D>("newLines");
            shadow = Content.Load<Texture2D>("newShadow");
            paddleEffect = Content.Load<Effect>("paddlesEffects");
            font = Content.Load<SpriteFont>("Font");
            font1 = Content.Load<SpriteFont>("FontGO");
            ballhits = Content.Load<Song>("ball");
            songGameOver = Content.Load<Song>("game_over");



            Random rand = new Random();

            paddles[0] = new Rectangle(PADDLE0_ORIGIN);
            paddles[1] = new Rectangle(PADDLE1_ORIGIN);
            sphere = new Ball(ORIGIN, new Vector3(0, 0, randomStart(rand)));



            VertexPositionNormalTexture[] skyBoxVertices = new VertexPositionNormalTexture[36]
            {
                // Front of skyBox (Behind camera at start) 
            new VertexPositionNormalTexture(new Vector3(1, -1, 1), -Vector3.UnitZ, new Vector2(TWO_THIRD, THREE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, 1, 1), -Vector3.UnitZ, new Vector2(TWO_THIRD, 1)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, 1), -Vector3.UnitZ, new Vector2(ONE_THIRD, 1)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, 1), -Vector3.UnitZ, new Vector2(ONE_THIRD, 1)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, 1), -Vector3.UnitZ, new Vector2(ONE_THIRD, THREE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, -1, 1), -Vector3.UnitZ, new Vector2(TWO_THIRD, THREE_FOURTH)),

            // Back of skyBox (In front of camera at start)
            new VertexPositionNormalTexture(new Vector3(-1, 1, -1), Vector3.UnitZ, new Vector2(ONE_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, 1, -1), Vector3.UnitZ, new Vector2(TWO_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, -1, -1), Vector3.UnitZ, new Vector2(TWO_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(1, -1, -1), Vector3.UnitZ, new Vector2(TWO_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, -1), Vector3.UnitZ, new Vector2(ONE_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, -1), Vector3.UnitZ, new Vector2(ONE_THIRD, ONE_FOURTH)),

            // Top of skyBox 
            new VertexPositionNormalTexture(new Vector3(-1, 1, 1), -Vector3.UnitY, new Vector2(ONE_THIRD, 0)),
            new VertexPositionNormalTexture(new Vector3(1, 1, 1), -Vector3.UnitY, new Vector2(TWO_THIRD, 0)),
            new VertexPositionNormalTexture(new Vector3(1, 1, -1), -Vector3.UnitY, new Vector2(TWO_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, 1, -1), -Vector3.UnitY, new Vector2(TWO_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, -1), -Vector3.UnitY, new Vector2(ONE_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, 1), -Vector3.UnitY, new Vector2(ONE_THIRD, 0)),

            // Bottom of skyBox
            new VertexPositionNormalTexture(new Vector3(1, -1, 1), Vector3.UnitY, new Vector2(TWO_THIRD, THREE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, 1), Vector3.UnitY, new Vector2(ONE_THIRD, THREE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, -1), Vector3.UnitY, new Vector2(ONE_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, -1), Vector3.UnitY, new Vector2(ONE_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(1, -1, -1), Vector3.UnitY, new Vector2(TWO_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(1, -1, 1), Vector3.UnitY, new Vector2(TWO_THIRD, THREE_FOURTH)),

            // Left of skyBox
            new VertexPositionNormalTexture(new Vector3(-1, -1, 1), Vector3.UnitX, new Vector2(0, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, 1), Vector3.UnitX, new Vector2(0, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, -1), Vector3.UnitX, new Vector2(ONE_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, -1), Vector3.UnitX, new Vector2(ONE_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, -1), Vector3.UnitX, new Vector2(ONE_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, 1), Vector3.UnitX, new Vector2(0, ONE_HALF)),

            // Right of skyBox
            new VertexPositionNormalTexture(new Vector3(1, 1, -1), -Vector3.UnitX, new Vector2(TWO_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, 1, 1), -Vector3.UnitX, new Vector2(1, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, -1, 1), -Vector3.UnitX, new Vector2(1, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(1, -1, 1), -Vector3.UnitX, new Vector2(1, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(1, -1, -1), -Vector3.UnitX, new Vector2(TWO_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(1, 1, -1), -Vector3.UnitX, new Vector2(TWO_THIRD, ONE_FOURTH))
            };

            skyBoxVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTexture), 36, BufferUsage.WriteOnly);
            skyBoxVertexBuffer.SetData<VertexPositionNormalTexture>(skyBoxVertices);


            VertexPositionNormalTexture[] fieldVertices = new VertexPositionNormalTexture[36]
            {
                // Front of field (Behind camera at start) 
            new VertexPositionNormalTexture(new Vector3(1, -1, 2), -Vector3.UnitZ, new Vector2(TWO_THIRD, THREE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, 1, 2), -Vector3.UnitZ, new Vector2(TWO_THIRD, 1)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, 2), -Vector3.UnitZ, new Vector2(ONE_THIRD, 1)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, 2), -Vector3.UnitZ, new Vector2(ONE_THIRD, 1)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, 2), -Vector3.UnitZ, new Vector2(ONE_THIRD, THREE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, -1, 2), -Vector3.UnitZ, new Vector2(TWO_THIRD, THREE_FOURTH)),

            // Back of field (In front of camera at start)
            new VertexPositionNormalTexture(new Vector3(-1, 1, -2), Vector3.UnitZ, new Vector2(ONE_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, 1, -2), Vector3.UnitZ, new Vector2(TWO_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, -1, -2), Vector3.UnitZ, new Vector2(TWO_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(1, -1, -2), Vector3.UnitZ, new Vector2(TWO_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, -2), Vector3.UnitZ, new Vector2(ONE_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, -2), Vector3.UnitZ, new Vector2(ONE_THIRD, ONE_FOURTH)),

            // Top of field 
            // Rotated for lines
            new VertexPositionNormalTexture(new Vector3(-1, 1, 2), -Vector3.UnitY, new Vector2(TWO_THIRD, 0)),
            new VertexPositionNormalTexture(new Vector3(1, 1, 2), -Vector3.UnitY, new Vector2(TWO_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, 1, -2), -Vector3.UnitY, new Vector2(ONE_THIRD, 0)),
            new VertexPositionNormalTexture(new Vector3(1, 1, -2), -Vector3.UnitY, new Vector2(ONE_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, -2), -Vector3.UnitY, new Vector2(ONE_THIRD, 0)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, 2), -Vector3.UnitY, new Vector2(TWO_THIRD, ONE_FOURTH)),

            // Bottom of field
            // Rotated for lines
            new VertexPositionNormalTexture(new Vector3(1, -1, 2), Vector3.UnitY, new Vector2(ONE_THIRD, THREE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, 2), Vector3.UnitY, new Vector2(ONE_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, -2), Vector3.UnitY, new Vector2(TWO_THIRD, THREE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, -2), Vector3.UnitY, new Vector2(TWO_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(1, -1, -2), Vector3.UnitY, new Vector2(TWO_THIRD, THREE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, -1, 2), Vector3.UnitY, new Vector2(ONE_THIRD, ONE_HALF)),

            // Left of field
            new VertexPositionNormalTexture(new Vector3(-1, -1, 2), Vector3.UnitX, new Vector2(0, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, 2), Vector3.UnitX, new Vector2(0, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, -2), Vector3.UnitX, new Vector2(ONE_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(-1, 1, -2), Vector3.UnitX, new Vector2(ONE_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, -2), Vector3.UnitX, new Vector2(ONE_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(-1, -1, 2), Vector3.UnitX, new Vector2(0, ONE_HALF)),

            // Right of field
            new VertexPositionNormalTexture(new Vector3(1, 1, -2), -Vector3.UnitX, new Vector2(TWO_THIRD, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, 1, 2), -Vector3.UnitX, new Vector2(1, ONE_FOURTH)),
            new VertexPositionNormalTexture(new Vector3(1, -1, 2), -Vector3.UnitX, new Vector2(1, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(1, -1, 2), -Vector3.UnitX, new Vector2(1, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(1, -1, -2), -Vector3.UnitX, new Vector2(TWO_THIRD, ONE_HALF)),
            new VertexPositionNormalTexture(new Vector3(1, 1, -2), -Vector3.UnitX, new Vector2(TWO_THIRD, ONE_FOURTH))
            };

            fieldBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTexture), 36, BufferUsage.WriteOnly);
            fieldBuffer.SetData<VertexPositionNormalTexture>(fieldVertices);



        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            Random rand = new Random();


            if (Keyboard.GetState().IsKeyDown(Keys.D2) && twoPlayer == false)
            {
                twoPlayer = true;
                playerPoints = 0;
                enemyPoints = 0;
                sphere.position = ORIGIN;
                sphere.velocity = new Vector3(0, 0, randomStart(rand));
                paddles[0].position = PADDLE0_ORIGIN;
                paddles[1].position = PADDLE1_ORIGIN;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.D1) && twoPlayer == true)
            {
                twoPlayer = false;
                playerPoints = 0;
                enemyPoints = 0;
                sphere.position = ORIGIN;
                sphere.velocity = new Vector3(0, 0, randomStart(rand));
                paddles[0].position = PADDLE0_ORIGIN;
                paddles[1].position = PADDLE1_ORIGIN;
            }


            // Is it our turn?
            if (sphere.velocity.Z < 0)
                playerTurn = false;
            else
                playerTurn = true;

            // Is the game over?
            if (playerPoints == 5 || enemyPoints == 5)
            {
                gameOver = true;
                if (gameoverSound == false)
                {
                    MediaPlayer.Play(songGameOver);
                    gameoverSound = true;
                }
                sphere.position.Y -= 0.05f;
                if (sphere.position.Y <= -BORDER.Y + BALL_RADIUS)
                    sphere.position.Y = -BORDER.Y + BALL_RADIUS;
            }

            // Single player
            if (twoPlayer == false)
            {

                // Move player paddle
                if (Keyboard.GetState().IsKeyDown(Keys.W))
                    paddles[0].position.Y += MOVE_SCALE;
                if (Keyboard.GetState().IsKeyDown(Keys.A))
                    paddles[0].position.X -= MOVE_SCALE;
                if (Keyboard.GetState().IsKeyDown(Keys.S))
                    paddles[0].position.Y -= MOVE_SCALE;
                if (Keyboard.GetState().IsKeyDown(Keys.D))
                    paddles[0].position.X += MOVE_SCALE;

                if (paddles[0].position.X > BORDER_PADDLE - paddleDim.X)
                    paddles[0].position.X = BORDER_PADDLE - paddleDim.X;
                if (paddles[0].position.X < -BORDER_PADDLE + paddleDim.X)
                    paddles[0].position.X = -BORDER_PADDLE + paddleDim.X;
                if (paddles[0].position.Y > BORDER_PADDLE - paddleDim.Y)
                    paddles[0].position.Y = BORDER_PADDLE - paddleDim.Y;
                if (paddles[0].position.Y < -BORDER_PADDLE + paddleDim.Y)
                    paddles[0].position.Y = -BORDER_PADDLE + paddleDim.Y;

                // Camera location
                cameraPosition = new Vector3((float)(95 * Math.Sin(axisYRotate)), 0f, (float)(95 * Math.Cos(axisYRotate)));
                view = Matrix.CreateLookAt(cameraPosition, ORIGIN, Vector3.UnitY);

                Vector3 tempCoords = ORIGIN;


                // Move camera
                if (Keyboard.GetState().IsKeyDown(Keys.Left))
                    axisYRotate += ANGLE;
                if (Keyboard.GetState().IsKeyDown(Keys.Right))
                    axisYRotate -= ANGLE;

                // Finds the target location and calls enemy paddle to move
                if (playerTurn == false && coordsFound == false && gameOver == false|| positioned == false && playerTurn == false && gameOver == false)
                {
                    tempCoords = findGoal(sphere);
                    coordsFound = true;
                    paddles[1].position = moveAI(paddles[1].position, tempCoords);
                }

                

            }

            // Two player
            if (twoPlayer == true)
            {
                // Camera location
                view = Matrix.CreateLookAt(new Vector3(80f, 0f, 0f), ORIGIN, Vector3.UnitY);


                // Move player paddle
                if (Keyboard.GetState().IsKeyDown(Keys.W))
                    paddles[0].position.Y += MOVE_SCALE;
                if (Keyboard.GetState().IsKeyDown(Keys.D))
                    paddles[0].position.X -= MOVE_SCALE;
                if (Keyboard.GetState().IsKeyDown(Keys.S))
                    paddles[0].position.Y -= MOVE_SCALE;
                if (Keyboard.GetState().IsKeyDown(Keys.A))
                    paddles[0].position.X += MOVE_SCALE;

                if (paddles[0].position.X > BORDER_PADDLE - paddleDim.X)
                    paddles[0].position.X = BORDER_PADDLE - paddleDim.X;
                if (paddles[0].position.X < -BORDER_PADDLE + paddleDim.X)
                    paddles[0].position.X = -BORDER_PADDLE + paddleDim.X;
                if (paddles[0].position.Y > BORDER_PADDLE - paddleDim.Y)
                    paddles[0].position.Y = BORDER_PADDLE - paddleDim.Y;
                if (paddles[0].position.Y < -BORDER_PADDLE + paddleDim.Y)
                    paddles[0].position.Y = -BORDER_PADDLE + paddleDim.Y;

                // Move Second paddle
                if (Keyboard.GetState().IsKeyDown(Keys.Up))
                    paddles[1].position.Y += MOVE_SCALE;
                if (Keyboard.GetState().IsKeyDown(Keys.Left))
                    paddles[1].position.X -= MOVE_SCALE;
                if (Keyboard.GetState().IsKeyDown(Keys.Down))
                    paddles[1].position.Y -= MOVE_SCALE;
                if (Keyboard.GetState().IsKeyDown(Keys.Right))
                    paddles[1].position.X += MOVE_SCALE;

                if (paddles[1].position.X > BORDER.X - paddleDim.X)
                    paddles[1].position.X = BORDER.X - paddleDim.X;
                if (paddles[1].position.X < -BORDER.X + paddleDim.X)
                    paddles[1].position.X = -BORDER.X + paddleDim.X;
                if (paddles[1].position.Y > BORDER.Y - paddleDim.Y)
                    paddles[1].position.Y = BORDER.Y - paddleDim.Y;
                if (paddles[1].position.Y < -BORDER.Y + paddleDim.Y)
                    paddles[1].position.Y = -BORDER.Y + paddleDim.Y;
            }

            // Checks if the game is over
            if (gameOver == false)
            {
                // Move ball
                sphere.position += sphere.velocity * gameTime.ElapsedGameTime.Milliseconds / 1000f;
                if (sphere.position.X > BORDER.X -BALL_RADIUS|| sphere.position.X < -BORDER.X+BALL_RADIUS)
                    sphere.velocity.X *= -1;
                if (sphere.position.Y > BORDER.Y - BALL_RADIUS || sphere.position.Y < -BORDER.Y+BALL_RADIUS)
                    sphere.velocity.Y *= -1;
            }


            // Bounce ball off player paddle
            if (sphere.position.Z > BORDER.Z-BALL_RADIUS)
            {
                if (Math.Abs(paddles[0].position.X - sphere.position.X) < 5 && Math.Abs(paddles[0].position.Y - sphere.position.Y) < 5 && playerTurn == true)
                {
                    sphere.velocity = bounce(sphere.position, paddles[0].position, sphere.velocity);
                    sphere.velocity.Z = -10;
                    playerTurn = false;
                    coordsFound = false;
                    positioned = false;
                    MediaPlayer.Play(ballhits);
                }
                // Give enemy or player 2 point
                if (Math.Abs(paddles[0].position.X - sphere.position.X) > 5 || Math.Abs(paddles[0].position.Y - sphere.position.Y) > 5 && playerTurn == true)
                {
                    enemyPoints += 1;
                    sphere.position = ORIGIN;
                    sphere.velocity = new Vector3(0f, 0f, randomStart(rand));
                }
            }

            //Bounce ball off enemy or player 2 paddle
            if (sphere.position.Z < -BORDER.Z+ BALL_RADIUS)
            {
                if (Math.Abs(paddles[1].position.X - sphere.position.X) < 5 && Math.Abs(paddles[1].position.Y - sphere.position.Y) < 5 && playerTurn == false)
                {
                    sphere.velocity = bounce(sphere.position, paddles[1].position, sphere.velocity);
                    sphere.velocity.Z = 10;
                    playerTurn = true;
                    MediaPlayer.Play(ballhits);
                }
                // Give player point
                if (Math.Abs(paddles[1].position.X - sphere.position.X) > 5 || Math.Abs(paddles[1].position.Y - sphere.position.Y) > 5 && playerTurn == false)
                {
                    playerPoints += 1;
                    sphere.position = ORIGIN;
                    sphere.velocity = new Vector3(0f, 0f, randomStart(rand));
                }
            }


            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            effect.World = world;
            effect.View = view;
            effect.Projection = projection;
            effect.TextureEnabled = true;
            effect.LightingEnabled = false;

            // Drawing SkyBox
            effect.Texture = skyBox;
            effect.World = Matrix.CreateScale(500) * Matrix.CreateTranslation(cameraPosition);
            GraphicsDevice.SetVertexBuffer(skyBoxVertexBuffer);

            RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.CullCounterClockwiseFace;
            GraphicsDevice.RasterizerState = rasterizerState;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 24);
            }

            // Drawing play field
            effect.Texture = field;
            effect.World = Matrix.CreateScale(20) * Matrix.CreateTranslation(ORIGIN);
            GraphicsDevice.SetVertexBuffer(fieldBuffer);

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 24);
            }

            // Drawing paddles
            effect.LightingEnabled = true;

            foreach (Rectangle rect in paddles)
            {
                Matrix world = Matrix.CreateScale(paddleDim) * Matrix.CreateTranslation(rect.position);
                foreach (ModelMesh mesh in cubeModel.Meshes)
                {
                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        part.Effect = paddleEffect;
                        paddleEffect.Parameters["World"].SetValue(mesh.ParentBone.Transform * world);
                        paddleEffect.Parameters["View"].SetValue(view);
                        paddleEffect.Parameters["WorldInverseTranspose"].SetValue(Matrix.Transpose(Matrix.Invert(mesh.ParentBone.Transform * world)));
                        paddleEffect.Parameters["Projection"].SetValue(projection);
                    }
                    mesh.Draw();
                }
            }

            // Draw sphere
            DrawModel(sphereModel, world, view, projection, sphere.position);


            // Draw sphere shadow
            Vector3 shadowPos = sphere.position;
            shadowPos.X -= SHADOW_ALIGN;
            shadowPos.Y = -BORDER.Y + 0.01f;
            shadowPos.Z -= SHADOW_ALIGN;
            shadowEffect.World = Matrix.CreateScale(1/75f) * Matrix.CreateRotationX(MathHelper.Pi / 2) * Matrix.CreateTranslation(shadowPos);
            shadowEffect.View = view;
            shadowEffect.Projection = projection;
            shadowEffect.TextureEnabled = true;
            shadowEffect.Texture = shadow;
            shadowEffect.DiffuseColor = Color.White.ToVector3();
           

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.DepthRead, RasterizerState.CullNone, shadowEffect);

            spriteBatch.Draw(shadow, new Vector2(0,0), Color.Black);
            
            spriteBatch.End();

            // Draws hint
            if (twoPlayer == false)
            {
                if (playerTurn == true)
                {
                    Vector3 hint = findGoal(sphere);
                    if (sphere.position.Z > ONEPLAYER_HINT)
                        DrawHint(hint);
                    
                }
            }
            if (twoPlayer == true)
            {
                Vector3 hint = findGoal(sphere);
                if (sphere.position.Z > TWOPLAYER_HINT && playerTurn == true)
                    DrawHint(hint);
                if (sphere.position.Z < -TWOPLAYER_HINT && playerTurn == false)
                    DrawHint(hint);
            }

            // Draws scoreboard and # of player instructions
            DrawString();
            // Draws game over screen
            DrawGameOver();

            // Resetting GraphicsDevice
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            base.Draw(gameTime);
        }

       // Basic lighting method
        private void DrawModel(Model model, Matrix world, Matrix view, Matrix projection, Vector3 position)
        {
            world = Matrix.CreateScale(2/3f) * Matrix.CreateTranslation(position); 
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = world;
                    effect.View = view;
                    effect.Projection = projection;


                }

                mesh.Draw();
            }
        }

        // Writes to the screen
        public void DrawString()
        {
            spriteBatch.Begin();
            spriteBatch.DrawString(font, "Player 1", new Vector2(25, 25), Color.White);
            spriteBatch.DrawString(font, playerPoints.ToString(), new Vector2(25, 50), Color.White);
            spriteBatch.DrawString(font, enemyPoints.ToString(), new Vector2(680, 50), Color.White);
            if (twoPlayer == false)
            {
                spriteBatch.DrawString(font, "Press 2 for Two Player", new Vector2(280, 25), Color.White);
                spriteBatch.DrawString(font, "Opponent", new Vector2(680, 25), Color.White);
            }
            if (twoPlayer == true)
            {
                spriteBatch.DrawString(font, "Player 1 for Single Player", new Vector2(275, 25), Color.White);
                spriteBatch.DrawString(font, "Player 2", new Vector2(680, 25), Color.White);
            }
            spriteBatch.End();

        }

        // Draws Game Over screen
        public void DrawGameOver()
        {
            spriteBatch.Begin();
            if (gameOver == true)
                spriteBatch.DrawString(font1, "GAME OVER", new Vector2(325, 250), Color.White);

            if (twoPlayer == false)
            {
                if (playerPoints == 5)
                    spriteBatch.DrawString(font, "YOU WIN!", new Vector2(350, 300), Color.White);
                if (enemyPoints == 5)
                    spriteBatch.DrawString(font, "YOU LOSE!", new Vector2(350, 300), Color.White);
            }

            if (twoPlayer == true)
            {
                if (playerPoints == 5)
                    spriteBatch.DrawString(font, "PLAYER 1 WINS!", new Vector2(320, 300), Color.White);
                if (enemyPoints == 5)
                    spriteBatch.DrawString(font, "PLAYER 2 WINS!", new Vector2(320, 300), Color.White);
            }
            spriteBatch.End();
        }
        
        // Draws Hint
        public void DrawHint(Vector3 position)
        {
            Vector3 hintPos = position;
            hintPos.X -= SHADOW_ALIGN;
            hintPos.Y -= SHADOW_ALIGN;
            if (sphere.velocity.Z > 0)
                hintPos.Z = BORDER.Z;
            if (sphere.velocity.Z < 0)
                hintPos.Z = -BORDER.Z;
            hintEffect.World = world;
            hintEffect.World = Matrix.CreateScale(1 / 75f) * Matrix.CreateTranslation(hintPos);
            hintEffect.View = view;
            hintEffect.Projection = projection;
            hintEffect.TextureEnabled = true;
            hintEffect.Texture = shadow;
            hintEffect.DiffuseColor = Color.Red.ToVector3();
            if (twoPlayer == true)
            {
                float diff = BORDER.Z - TWOPLAYER_HINT;
                hintAlpha = (diff - (Math.Abs(sphere.position.Z) - BORDER.Z)) / diff;
            }
            if (twoPlayer == false)
            {
                float diff = BORDER.Z - ONEPLAYER_HINT;
                hintAlpha = (diff - (Math.Abs(sphere.position.Z) - BORDER.Z)) / diff;
            }
            hintEffect.Alpha = hintAlpha * 0.5f;

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.DepthRead, RasterizerState.CullNone, hintEffect);

            spriteBatch.Draw(shadow, new Vector2(0, 0), Color.White);

            spriteBatch.End();
        }

    }
}
