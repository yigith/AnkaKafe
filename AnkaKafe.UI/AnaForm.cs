using AnkaKafe.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO; // dosyaya yazmak ve dosyadan okumak için gerekli
using System.Linq;
using System.Text;
using System.Text.Json; // json serialization için gerekli
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnkaKafe.UI
{
    public partial class AnaForm : Form
    {
        KafeVeri db;

        public AnaForm()
        {
            VerileriOku();
            InitializeComponent();
            Icon = Resource.AnkaKafe;
            masalarImageList.Images.Add("bos", Resource.bos);
            masalarImageList.Images.Add("dolu", Resource.dolu);
            MasalariOlustur();
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to?pivots=dotnet-5-0
        private void VerileriOku()
        {
            // verileri oku ve deserialize et
            try
            {
                string json = File.ReadAllText("veri.json");
                db = JsonSerializer.Deserialize<KafeVeri>(json);
            }
            // değer hata alırsan (dosya yoktur ya da bozuktur)
            catch (Exception)
            {
                db = new KafeVeri();
                OrnekUrunleriEkle();
            }
        }

        private void OrnekUrunleriEkle()
        {
            db.Urunler.Add(new Urun() { UrunAd = "Çay", BirimFiyat = 4.00m });
            db.Urunler.Add(new Urun() { UrunAd = "Simit", BirimFiyat = 5.00m });
        }

        private void MasalariOlustur()
        {
            ListViewItem lvi;
            for (int i = 1; i <= db.MasaAdet; i++)
            {
                lvi = new ListViewItem();
                lvi.Tag = i; // masa noyu her bir öğenin Tag property'sinde saklayalım
                lvi.Text = "Masa " + i;
                lvi.ImageKey = MasaDoluMu(i) ? "dolu" : "bos";
                lvwMasalar.Items.Add(lvi);
            }
        }

        private bool MasaDoluMu(int masaNo)
        {
            // verilen şartı sağlayan herhangi bir aktif sipariş var mı? varsa: true    yoksa: false
            return db.AktifSiparisler.Any(x => x.MasaNo == masaNo);
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == tsmiUrunler)
            {
                new UrunlerForm(db).ShowDialog();
            }
            else if (e.ClickedItem == tsmiGecmisSiparisler)
            {
                new GecmisSiparislerForm(db).ShowDialog();
            }
        }

        private void lvwMasalar_DoubleClick(object sender, EventArgs e)
        {
            ListViewItem lvi = lvwMasalar.SelectedItems[0];
            int masaNo = (int)lvi.Tag; // unboxing
            lvi.ImageKey = "dolu";
            // Eğer bu masada önceden bir sipariş yoksa oluştur
            Siparis siparis = SiparisBul(masaNo);

            if (siparis == null)
            {
                siparis = new Siparis() { MasaNo = masaNo };
                db.AktifSiparisler.Add(siparis);
            }

            // Bu siparişi başka bir formda aç
            SiparisForm siparisForm = new SiparisForm(db, siparis);

            // EVENT OLUŞTURMADA 5. ADIM: Event'e oluşturulan metotu atamak
            siparisForm.MasaTasindi += SiparisForm_MasaTasindi;

            siparisForm.ShowDialog();

            // Sipariş formu kapandıktan sonra sipariş durumunu kontrol et
            if (siparis.Durum != SiparisDurum.Aktif)
            {
                lvi.ImageKey = "bos";
            }
        }

        // EVENT OLUŞTURMADA 4. ADIM: Event'e atanacak metotu 
        // event delegesinin dönüş tipi ve argüman çeşitlerine uygun olarak oluşturmak
        private void SiparisForm_MasaTasindi(object sender, MasaTasindiEventArgs e)
        {
            foreach (ListViewItem lvi in lvwMasalar.Items)
            {
                int masaNo = (int)lvi.Tag;
                if (masaNo == e.EskiMasaNo)
                {
                    lvi.ImageKey = "bos";
                }
                else if (masaNo == e.YeniMasaNo)
                {
                    lvi.ImageKey = "dolu";
                }
            }
        }

        private Siparis SiparisBul(int masaNo)
        {
            // return db.AktifSiparisler.FirstOrDefault(s => s.MasaNo == masaNo);

            foreach (Siparis siparis in db.AktifSiparisler)
            {
                if (siparis.MasaNo == masaNo)
                {
                    return siparis;
                }
            }

            return null;
        }

        private void AnaForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            VerileriKaydet();
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to?pivots=dotnet-5-0
        private void VerileriKaydet()
        {
            var options = new JsonSerializerOptions() { WriteIndented = true }; // json'ı okunaklı (indentation ile) oluştur
            string json = JsonSerializer.Serialize(db, options);
            File.WriteAllText("veri.json", json);
        }
    }
}
