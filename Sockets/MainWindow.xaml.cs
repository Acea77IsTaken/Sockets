using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sockets
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isMyTurn = true;
        private Random random = new Random();
        private GameClient _client;
        private int _playerId;
        
        


        public MainWindow()
        {
            InitializeComponent();
            _client = new GameClient();
            _client.OnMessageReceived += HandleServerMessage;
            _client.Connect("192.168.68.128", 1234); // Cambia por tu IP local
            
        }
        private void ShowBattleEffect(string emoji)
        {
            BattleEffect.Text = emoji;
            BattleEffect.Opacity = 1;

            // Animación de escala
            var scaleAnimation = new DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(300));
            scaleAnimation.AutoReverse = true;
            EffectScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            EffectScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

            // Fade out
            var fadeAnimation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(1000));
            fadeAnimation.BeginTime = TimeSpan.FromMilliseconds(600);
            BattleEffect.BeginAnimation(OpacityProperty, fadeAnimation);
        }

        private void AddToLog(string message)
        {
            BattleLog.Text += $"{message}\n";
            LogScroll.ScrollToEnd();
        }

        private void HandleServerMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string[] parts = message.Split('|');
                string action = parts[0];
                int value = int.Parse(parts[1]);

                switch (action)
                {
                    case "CONNECT":
                        _playerId = value;
                        AddToLog($"Eres el Jugador {_playerId}");
                        isMyTurn = (_playerId == 1);
                        UpdateTurnUI();
                        break;

                    case "TURN":
                        isMyTurn = (value == _playerId);
                        UpdateTurnUI();
                        if (isMyTurn) AddToLog("¡Es tu turno!");
                        break;

                    case "ATTACK":
                        // El mensaje de ataque siempre viene del oponente
                        bool attackComesFromOpponent = (_playerId == 2 && parts.Length > 2 && parts[2] == "1") ||
                                                     (_playerId == 1 && parts.Length > 2 && parts[2] == "2");

                        // Siempre dañamos a nuestro propio personaje (el oponente atacó)
                        if (attackComesFromOpponent)
                        {
                            DamagePlayer(value, isPlayer1: _playerId == 1);
                            AddToLog($"⚔️ Oponente te ataca por {value} de daño!");
                            ShowBattleEffect("💢");
                        }
                        break;

                    case "DEFEND":
                        bool isOpponentDefend = (_playerId == 2);
                        AddToLog($"🛡️ {(isOpponentDefend ? "Oponente" : "Tú")} se defiende!");
                        ShowBattleEffect("🛡️");
                        break;

                    case "MAGIC":
                        bool isOpponentMagic = (_playerId == 2);
                        DamagePlayer(value, isPlayer1: !isOpponentMagic);
                        AddToLog($"🔮 {(isOpponentMagic ? "Oponente" : "Tú")} lanza un hechizo ({value} daño)!");
                        ShowBattleEffect("✨");
                        break;

                    case "HEAL":
                        bool isOpponentHeal = (_playerId == 2);
                        HealPlayer(value, isPlayer1: !isOpponentHeal);
                        AddToLog($"🧪 {(isOpponentHeal ? "Oponente" : "Tú")} usa una poción (+{value} vida)!");
                        ShowBattleEffect("🧪");
                        break;
                }
            });
        }

        private void DamagePlayer(int damage, bool isPlayer1)
        {
            // Asegurarnos de que siempre dañamos al personaje correcto
            if ((_playerId == 1 && isPlayer1) || (_playerId == 2 && !isPlayer1))
            {
                // Dañar al jugador local
                Player1Health.Value = Math.Max(0, Player1Health.Value - damage);
                Player1HealthText.Text = $"{Player1Health.Value}/100";
            }
            else
            {
                // Dañar al oponente (visualización local)
                Player2Health.Value = Math.Max(0, Player2Health.Value - damage);
                Player2HealthText.Text = $"{Player2Health.Value}/100";
            }
        }

        private void HealPlayer(int amount, bool isPlayer1)
        {
            if (isPlayer1)
            {
                Player1Health.Value = Math.Min(100, Player1Health.Value + amount);
                Player1HealthText.Text = $"{Player1Health.Value}/100";
            }
            else
            {
                Player2Health.Value = Math.Min(100, Player2Health.Value + amount);
                Player2HealthText.Text = $"{Player2Health.Value}/100";
            }
        }

        private void UpdateTurnUI()
        {
            TurnText.Text = isMyTurn ? "🎯 TU TURNO" : "⏳ ESPERANDO";
            TurnText.Foreground = isMyTurn ? Brushes.LimeGreen : Brushes.Orange;
        }

        // Botones modificados para enviar acciones al servidor
        private void AttackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isMyTurn) return;

            int damage = random.Next(15, 30);
            // Enviamos nuestro ID junto con el ataque
            _client.SendAction($"ATTACK",damage);

            // Mostramos el efecto visual localmente
            ShowBattleEffect("💥");
            AddToLog($"⚔️ Atacas al oponente por {damage} de daño!");

            // Cambiamos el turno localmente (el servidor confirmará)
            isMyTurn = false;
            UpdateTurnUI();
        }

        private void DefendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isMyTurn) return;

            _client.SendAction("DEFEND", 0);
            ShowBattleEffect("🛡️");
        }

        private void MagicBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isMyTurn || Player1Mana.Value < 20) return;

            int damage = random.Next(25, 40);
            _client.SendAction("MAGIC", damage);
            Player1Mana.Value -= 20;
            Player1ManaText.Text = $"{Player1Mana.Value}/100";
            ShowBattleEffect("✨");
        }

        private void ItemBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isMyTurn) return;

            int healing = random.Next(20, 35);
            _client.SendAction("HEAL", healing);
            ShowBattleEffect("🧪");
        }
    }
}

