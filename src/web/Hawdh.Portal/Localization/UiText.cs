using System.Globalization;

namespace Hawdh.Portal.Localization;

public sealed class UiText
{
    private static readonly Dictionary<string, (string Ar, string Fr)> Text = new()
    {
        ["brand"] = ("AGIRE", "AGIRE"), ["brandSub"] = ("الموارد المائية", "Ressources en eau"),
        ["morningGreeting"] = ("صباح الخير", "Bonjour"), ["eveningGreeting"] = ("مساء الخير", "Bonsoir"),
        ["registerConfirmationTitle"] = ("تأكيد الحساب", "Confirmation du compte"),
        ["passwordRecoveryTitle"] = ("استعادة كلمة المرور", "Récupération du mot de passe"),
        ["backToLogin"] = ("العودة إلى تسجيل الدخول", "Retour à la connexion"),
        ["passwordRecoveryHint"] = ("أدخل البريد الإلكتروني المرتبط بحسابك وسنرسل رابطاً لإعادة تعيين كلمة المرور إذا كان الحساب مؤهلاً.", "Saisissez l’adresse e-mail associée à votre compte. Si celui-ci est admissible, un lien de réinitialisation vous sera envoyé."),
        ["passwordRecoverySent"] = ("إذا كان البريد مرتبطاً بحساب نشط ومؤكد، فستصلك رسالة لإعادة تعيين كلمة المرور.", "Si cette adresse est associée à un compte actif et confirmé, vous recevrez un message de réinitialisation."),
        ["sendRecoveryLink"] = ("إرسال رابط الاستعادة", "Envoyer le lien de réinitialisation"),
        ["confirmationEmailSent"] = ("تحقق من بريدك الإلكتروني لفتح رابط تأكيد الحساب.", "Consultez votre boîte e-mail pour ouvrir le lien de confirmation."),
        ["forgotPassword"] = ("نسيت كلمة المرور؟", "Mot de passe oublié ?"),
        ["resetPasswordTitle"] = ("تعيين كلمة مرور جديدة", "Définir un nouveau mot de passe"),
        ["resetPasswordHint"] = ("أدخل بريد الحساب وكلمة المرور الجديدة لتأكيد التغيير.", "Saisissez l’adresse e-mail du compte et votre nouveau mot de passe pour confirmer la modification."),
        ["newPassword"] = ("كلمة المرور الجديدة", "Nouveau mot de passe"),
        ["confirmNewPassword"] = ("تأكيد كلمة المرور الجديدة", "Confirmer le nouveau mot de passe"),
        ["resetPasswordButton"] = ("تعيين كلمة المرور", "Réinitialiser le mot de passe"),
        ["passwordResetComplete"] = ("تم تغيير كلمة المرور. يمكنك تسجيل الدخول الآن.", "Votre mot de passe a été modifié. Vous pouvez vous connecter."),
        ["invalidPasswordResetLink"] = ("رابط إعادة تعيين كلمة المرور غير صالح أو انتهت صلاحيته.", "Le lien de réinitialisation est invalide ou a expiré."),
        ["backToLoginAction"] = ("تسجيل الدخول", "Se connecter"),
        ["emailAlreadyInUse"] = ("هذا البريد الإلكتروني مرتبط بحساب آخر.", "Cette adresse e-mail est déjà associée à un autre compte."),
        ["emailChangeConfirmTitle"] = ("تأكيد تغيير البريد الإلكتروني", "Confirmer le changement d’adresse e-mail"),
        ["emailChangeInvalidLink"] = ("رابط تأكيد البريد غير صالح أو انتهت صلاحيته.", "Le lien de confirmation est invalide ou a expiré."),
        ["emailChangeUserMissing"] = ("تعذر العثور على الحساب المرتبط بهذا الرابط.", "Le compte associé à ce lien est introuvable."),
        ["emailChangeFailed"] = ("تعذر تغيير البريد الإلكتروني. قد يكون العنوان مستخدماً أو انتهت صلاحية الرابط.", "Impossible de modifier l’adresse. Elle est peut-être déjà utilisée ou le lien a expiré."),
        ["emailChangeComplete"] = ("تم تأكيد البريد الإلكتروني وتحديثه بنجاح. سجل الدخول بالبريد الجديد.", "L’adresse e-mail a été confirmée et mise à jour. Connectez-vous avec la nouvelle adresse."),
        ["resendConfirmationTitle"] = ("إعادة إرسال تأكيد البريد", "Renvoyer la confirmation par e-mail"),
        ["resendConfirmationHint"] = ("أدخل بريدك الإلكتروني لإعادة إرسال رابط التأكيد إذا كان الحساب يحتاج إلى ذلك.", "Saisissez votre adresse e-mail pour renvoyer le lien si le compte en a besoin."),
        ["resendConfirmationAction"] = ("إعادة إرسال الرابط", "Renvoyer le lien"),
        ["confirmationEmailSentIfAvailable"] = ("إذا كان الحساب يحتاج إلى تأكيد البريد، فستصلك رسالة على العنوان المسجل.", "Si le compte doit confirmer son adresse, un message sera envoyé à l’adresse enregistrée."),
        ["menu"] = ("القائمة الرئيسية", "MENU PRINCIPAL"), ["dashboard"] = ("الرئيسية", "Vue d’ensemble"),
        ["customers"] = ("الزبائن", "Clients"), ["invoices"] = ("الفواتير والتحصيل", "Factures et paiements"),
        ["selectCustomer"] = ("اختر زبوناً قبل حفظ الفاتورة.", "Sélectionnez un client avant d’enregistrer la facture."),
        ["completeInvoiceFields"] = ("أكمل بيانات الفاتورة بشكل صحيح.", "Complétez correctement les informations de la facture."),
        ["invoiceCreated"] = ("تم حفظ الفاتورة بنجاح.", "Facture enregistrée avec succès."),
        ["invoiceSaveFailed"] = ("تعذر حفظ الفاتورة. تحقق من البيانات وحاول مرة أخرى.", "Impossible d’enregistrer la facture. Vérifiez les données et réessayez."),
        ["usage"] = ("استهلاك المياه", "Consommation d’eau"), ["reports"] = ("التقارير", "Rapports"),
        ["connected"] = ("النظام متصل", "Système connecté"), ["secure"] = ("مساحة عمل آمنة", "Espace sécurisé"),
        ["workspace"] = ("مساحة العمل", "Espace de travail"), ["switch"] = ("FR", "عربي"),
        ["logout"] = ("تسجيل الخروج", "Déconnexion"), ["overview"] = ("نظرة عامة على المؤسسة", "Vue générale de l’organisation"),
        ["overviewHint"] = ("تابع نشاط الزبائن والفواتير واستهلاك المياه من مكان واحد.", "Suivez les clients, factures et consommation depuis un seul espace."),
        ["newCustomer"] = ("إضافة زبون", "Ajouter un client"), ["newInvoice"] = ("فاتورة جديدة", "Nouvelle facture"),
        ["invalidSubscriptionYear"] = ("سنة الاشتراك يجب ألا تتجاوز السنة الحالية.", "L’année d’abonnement ne peut pas dépasser l’année en cours."),
        ["tariffSettings"] = ("تعرفة المياه", "Tarif de l’eau"), ["tariffSettingsTitle"] = ("إدارة تعرفة المياه", "Gestion du tarif de l’eau"),
        ["tariffSettingsHint"] = ("حدد التعرفة المركزية المعتمدة. تُستخدم تلقائياً في كل قراءة وفاتورة استهلاك جديدة.", "Définissez le tarif officiel central, appliqué à chaque nouveau relevé et à chaque facture de consommation."),
        ["tariffNotConfigured"] = ("لم تُحدد التعرفة بعد", "Tarif non configuré"), ["tariffSaved"] = ("تم حفظ التعرفة المركزية", "Tarif central enregistré"),
        ["tariffChangedReview"] = ("تغيرت التعرفة المركزية. راجع المبلغ المحسوب ثم احفظ القراءة مجدداً.", "Le tarif central a changé. Vérifiez le montant calculé puis enregistrez à nouveau le relevé."),
        ["tariffRequired"] = ("يجب على المدير تحديد تعرفة صحيحة قبل تسجيل قراءات الاستهلاك.", "Un administrateur doit définir un tarif valide avant la saisie des relevés."),
        ["tariffPrecision"] = ("أدخل التعرفة بأربع منازل عشرية كحد أقصى.", "Saisissez au maximum quatre décimales pour le tarif."),
        ["tariffRate"] = ("تعرفة المتر المكعب", "Tarif par mètre cube"), ["tariffRateHint"] = ("القيمة بالدينار الجزائري لكل متر مكعب (م³).", "Montant en dinars algériens par mètre cube (m³)."),
        ["tariffUpdatedBy"] = ("آخر تعديل بواسطة", "Dernière modification par"), ["tariffNotSetHint"] = ("لم تُعتمد تعرفة بعد. لا يمكن تسجيل قراءة أو إنشاء فاتورة استهلاك حتى يحددها مدير النظام.", "Aucun tarif n’est encore défini. Aucun relevé ni facture de consommation ne peut être enregistré avant l’intervention d’un administrateur."),
        ["meterNumber"] = ("رقم العداد", "Numéro du compteur"), ["subscriptionYear"] = ("سنة الاشتراك", "Année d’abonnement"),
        ["hideSidebar"] = ("إخفاء القائمة الجانبية", "Masquer la barre latérale"), ["showSidebar"] = ("إظهار القائمة الجانبية", "Afficher la barre latérale"),
        ["customersTotal"] = ("إجمالي الزبائن", "Total clients"), ["outstanding"] = ("المبالغ المستحقة", "Montants dus"),
        ["collected"] = ("المبالغ المحصلة", "Montants encaissés"), ["usageMonth"] = ("استهلاك هذا الشهر", "Consommation du mois"),
        ["financialHealth"] = ("الموقف المالي", "Situation financière"), ["financialHint"] = ("توزيع الفواتير حسب السداد", "Répartition des factures selon leur règlement"),
        ["paid"] = ("مسدد", "Réglé"), ["open"] = ("مستحق", "À régler"), ["partial"] = ("جزئي", "Partiel"),
        ["trend"] = ("حركة التحصيل", "Évolution des encaissements"), ["last6"] = ("آخر 6 أشهر", "6 derniers mois"),
        ["dashboardInsights"] = ("مؤشرات الأداء", "Indicateurs de performance"), ["insightsHint"] = ("استكشف التحصيل والاستهلاك وحالة الفواتير عبر مخططات تفاعلية.", "Explorez les encaissements, la consommation et les factures avec des graphiques interactifs."),
        ["liveOverview"] = ("ملخص المؤسسة", "Vue de l’organisation"), ["chartRange"] = ("الفترة الزمنية للمخطط", "Période du graphique"), ["previousSlide"] = ("المؤشر السابق", "Indicateur précédent"), ["nextSlide"] = ("المؤشر التالي", "Indicateur suivant"), ["showSlide"] = ("اعرض المؤشر", "Afficher l’indicateur"),
        ["usageTrend"] = ("اتجاه استهلاك المياه", "Évolution de la consommation d’eau"), ["last12"] = ("آخر 12 شهراً", "12 derniers mois"), ["averageMonthlyUsage"] = ("متوسط الاستهلاك الشهري", "Consommation mensuelle moyenne"), ["paidInvoiceRate"] = ("نسبة الفواتير المسددة", "Taux de factures réglées"),
        ["recent"] = ("أحدث الفواتير", "Factures récentes"), ["viewAll"] = ("عرض الكل", "Tout afficher"),
        ["invoiceNo"] = ("رقم الفاتورة", "N° facture"), ["customer"] = ("الزبون", "Client"),
        ["issueDate"] = ("تاريخ الإصدار", "Date d’émission"), ["amount"] = ("الإجمالي", "Total"),
        ["remaining"] = ("المتبقي", "Reste dû"), ["status"] = ("الحالة", "Statut"),
        ["noInvoices"] = ("لا توجد فواتير بعد", "Aucune facture pour le moment"),
        ["noInvoicesHint"] = ("ستظهر أحدث الفواتير هنا بعد إضافتها.", "Les factures apparaîtront ici après leur création."),
        ["search"] = ("بحث", "Rechercher"), ["searchCustomers"] = ("ابحث بالاسم أو الرمز أو الهاتف", "Nom, code ou téléphone"),
        ["customerDirectory"] = ("دليل الزبائن", "Répertoire clients"), ["customerHint"] = ("إدارة بيانات الزبائن ومتابعة أرصدتهم.", "Coordonnées et soldes des clients."),
        ["code"] = ("رمز الزبون", "Code client"), ["name"] = ("اسم الزبون", "Nom du client"), ["phone"] = ("رقم الهاتف", "Téléphone"),
        ["location"] = ("الموقع / الحقل", "Secteur / parcelle"), ["balance"] = ("الرصيد المستحق", "Solde dû"),
        ["customerReport"] = ("تقرير سجل الزبائن", "Rapport du registre clients"), ["previewCustomers"] = ("معاينة وطباعة سجل الزبائن", "Aperçu et impression du registre clients"),
        ["rowNumber"] = ("الرقم", "N°"),
        ["printReport"] = ("طباعة التقرير", "Imprimer le rapport"), ["printPopupBlocked"] = ("تعذر فتح نافذة الطباعة. اسمح بالنوافذ المنبثقة ثم أعد المحاولة.", "Impossible d’ouvrir la fenêtre d’impression. Autorisez les fenêtres contextuelles puis réessayez."), ["exportCustomersExcel"] = ("تنزيل Excel", "Télécharger Excel"),
        ["reportGeneratedAt"] = ("تاريخ إعداد التقرير", "Rapport généré le"), ["subscription"] = ("الاشتراك", "Abonnement"),
        ["latestReadingPeriod"] = ("فترة آخر قراءة", "Période du dernier relevé"), ["noReadingRecorded"] = ("لا توجد قراءة مسجلة", "Aucun relevé enregistré"),
        ["noDebt"] = ("لا توجد ديون", "Aucune dette"), ["customerCount"] = ("عدد الزبائن", "Nombre de clients"),
        ["otherCurrencyDebts"] = ("إجمالي الديون بالعملات الأخرى", "Total des dettes dans les autres devises"),
        ["grandTotal"] = ("الإجمالي العام", "Total général"),
        ["totalDebt"] = ("إجمالي الديون", "Total des dettes"), ["customerRegister"] = ("سجل الزبائن", "Registre des clients"),
        ["accountState"] = ("حالة الحساب", "État du compte"), ["active"] = ("نشط", "Actif"), ["inactive"] = ("موقوف", "Suspendu"),
        ["actions"] = ("إجراءات", "Actions"), ["edit"] = ("تعديل", "Modifier"), ["deactivate"] = ("إيقاف", "Suspendre"),
        ["activate"] = ("تفعيل", "Activer"), ["noCustomers"] = ("لا يوجد زبائن مسجلون", "Aucun client enregistré"),
        ["noCustomersHint"] = ("ابدأ بإضافة أول زبون إلى سجل المؤسسة.", "Ajoutez le premier client au registre."),
        ["save"] = ("حفظ", "Enregistrer"), ["cancel"] = ("إلغاء", "Annuler"), ["close"] = ("إغلاق", "Fermer"), ["confirmAction"] = ("تأكيد الإجراء", "Confirmer l’action"), ["notes"] = ("ملاحظات", "Notes"),
        ["customerAdded"] = ("تمت إضافة الزبون بنجاح", "Client ajouté"), ["customerSaved"] = ("تم حفظ بيانات الزبون", "Coordonnées enregistrées"),
        ["confirmDeactivate"] = ("هل تريد إيقاف حساب هذا الزبون؟", "Suspendre ce compte client ?"),
        ["invoiceRegister"] = ("أرشيف الفواتير الشهري", "Archives mensuelles des factures"), ["invoiceHint"] = ("راجع فواتير أي شهر سابق، أو اعرض السجل الكامل.", "Consultez les factures de n’importe quel mois ou tout l’historique."),
        ["dueDate"] = ("تاريخ الاستحقاق", "Échéance"), ["paidAmount"] = ("المدفوع", "Payé"),
        ["recordPayment"] = ("تسجيل دفعة", "Enregistrer un paiement"), ["paymentAdded"] = ("تم تسجيل الدفعة", "Paiement enregistré"),
        ["paymentAmount"] = ("مبلغ الدفعة", "Montant du paiement"), ["paymentDate"] = ("تاريخ الدفع", "Date de paiement"),
        ["invoiceExists"] = ("رقم الفاتورة مستخدم من قبل. اختر رقمًا آخر.", "Ce numéro de facture existe déjà. Choisissez-en un autre."),
        ["invalidInvoiceDate"] = ("تاريخ الإصدار لا يمكن أن يكون في المستقبل، والاستحقاق لا يسبق الإصدار.", "La date d’émission ne peut pas être future et l’échéance ne peut pas précéder l’émission."),
        ["invalidPaymentDate"] = ("تاريخ الدفع يجب أن يكون بعد إصدار الفاتورة وألا يتجاوز اليوم.", "La date de paiement doit être comprise entre la date d’émission et aujourd’hui."),
        ["noData"] = ("لا توجد بيانات لعرضها", "Aucune donnée à afficher"), ["noChartData"] = ("لا توجد بيانات خلال هذه الفترة", "Aucune donnée sur cette période"), ["usageTitle"] = ("قراءات استهلاك المياه", "Relevés de consommation d’eau"),
        ["usageHint"] = ("اختر الزبون والشهر؛ تُحمّل آخر قراءة تلقائياً ويُحسب الاستهلاك والمبلغ وتصدر الفاتورة.", "Choisissez le client et le mois : le dernier relevé est repris automatiquement, puis la consommation et le montant sont calculés et facturés."),
        ["readingDate"] = ("تاريخ القراءة", "Date du relevé"), ["noInvoice"] = ("بدون فاتورة", "Sans facture"),
        ["previousReadingDate"] = ("تاريخ القراءة السابقة", "Date de l’ancien relevé"), ["currentReadingDate"] = ("تاريخ القراءة الحالية", "Date du relevé actuel"),
        ["period"] = ("الفترة", "Période"), ["previousReading"] = ("القراءة السابقة", "Index précédent"), ["initialReading"] = ("قراءة العداد عند بدء التسجيل", "Index initial du compteur"),
        ["currentReading"] = ("القراءة الحالية", "Index actuel"), ["consumption"] = ("الاستهلاك", "Consommation"),
        ["rate"] = ("سعر المتر", "Tarif unitaire"), ["charge"] = ("قيمة الاستهلاك", "Montant consommation"),
        ["recordReading"] = ("تسجيل قراءة", "Saisir un relevé"), ["reportsTitle"] = ("التقارير والتصدير", "Rapports et export"),
        ["downloadExcel"] = ("تنزيل تقرير Excel", "Télécharger le rapport Excel"), ["reportHint"] = ("ملف Excel للزبائن والفواتير والدفعات والاستهلاك.", "Classeur clients, factures, paiements et consommation."),
        ["previewReport"] = ("معاينة التقرير", "Aperçu du rapport"), ["previewAndPrint"] = ("معاينة وطباعة", "Aperçu et impression"), ["officialReport"] = ("تقرير رسمي", "Rapport officiel"),
        ["indicator"] = ("المؤشر", "Indicateur"), ["value"] = ("القيمة", "Valeur"),
        ["reference"] = ("المرجع", "Référence"), ["recordedBy"] = ("المسجل بواسطة", "Enregistré par"),
        ["reportPeriod"] = ("فترة التقرير", "Période du rapport"), ["reportPeriodHint"] = ("اختر شهراً من الأرشيف أو اعرض السجل كاملاً.", "Choisissez un mois archivé ou consultez tout l’historique."),
        ["allPeriods"] = ("كل الفترات", "Toutes les périodes"),
        ["excelWorkbook"] = ("ملف عمل منظم وجاهز للتحليل", "Classeur structuré, prêt à analyser"),
        ["excelWorkbookHint"] = ("تعكس التفاصيل الفترة المختارة، ويحتفظ الأرشيف الشهري بالسجل التاريخي كاملاً.", "Les détails suivent la période choisie ; l’archive mensuelle conserve tout l’historique."),
        ["sheetSummary"] = ("الملخص", "Synthèse"), ["sheetSummaryHint"] = ("مؤشرات الفترة وإجمالياتها", "Indicateurs et totaux de la période"),
        ["sheetCustomers"] = ("الزبائن", "Clients"), ["sheetCustomersHint"] = ("العداد والاشتراك والأرصدة", "Compteur, abonnement et soldes"),
        ["sheetInvoices"] = ("الفواتير", "Factures"), ["sheetInvoicesHint"] = ("الإجمالي والمدفوع والمتبقي", "Total, payé et solde restant"),
        ["sheetPayments"] = ("الدفعات", "Paiements"), ["sheetPaymentsHint"] = ("تواريخ الدفع ومراجع التحويل", "Dates de paiement et références"),
        ["sheetUsage"] = ("الاستهلاك", "Consommation"), ["sheetUsageHint"] = ("القراءات والاستهلاك والحالة", "Relevés, consommation et statut"),
        ["sheetArchive"] = ("الأرشيف الشهري", "Archives mensuelles"), ["sheetArchiveHint"] = ("ملخص تاريخي للفواتير والتحصيل", "Historique mensuel des factures et encaissements"),
        ["reportExportHint"] = ("البيانات المصدّرة تتبع صلاحيات دخولك والفترة المحددة.", "L’export respecte vos droits d’accès et la période sélectionnée."),
        ["loading"] = ("جارٍ تحميل التقرير…", "Chargement du rapport…"),
        ["loginTitle"] = ("تسجيل الدخول", "Connexion"), ["loginSubtitle"] = ("سجل دخولك إلى مساحة العمل الآمنة.", "Connectez-vous à votre espace sécurisé."),
        ["email"] = ("البريد الإلكتروني", "Adresse e-mail"), ["password"] = ("كلمة المرور", "Mot de passe"),
        ["remember"] = ("تذكرني على هذا الجهاز", "Se souvenir de moi sur cet appareil"), ["signIn"] = ("دخول إلى المنصة", "Accéder à la plateforme"),
        ["loginError"] = ("تعذر تسجيل الدخول. تحقق من البيانات وحاول مرة أخرى.", "Connexion impossible. Vérifiez vos identifiants."),
        ["loginFoot"] = ("الدخول متاح لموظفي المؤسسة المخولين فقط.", "Accès réservé au personnel autorisé."),
        ["systemName"] = ("الوكالة الوطنية للتسيير المدمج للموارد المائية", "Agence nationale de gestion intégrée des ressources en eau"),
        ["systemLine"] = ("إدارة دقيقة. خدمة مياه موثوقة.", "Gestion précise. Service d’eau fiable."),
        ["currency"] = ("دج", "DZD"), ["overdue"] = ("متأخرة", "En retard"),
        ["activeCustomers"] = ("الزبائن النشطون", "Clients actifs"), ["allStatuses"] = ("كل الحالات", "Tous les statuts"),
        ["debtor"] = ("مدين", "Débiteur"), ["description"] = ("البيان", "Description"),
        ["footerSecure"] = ("بيانات المؤسسة محفوظة ضمن مساحة العمل", "Données protégées dans l’espace de travail"),
        ["invoicesIssued"] = ("الفواتير الصادرة", "Factures émises"), ["noAccess"] = ("ليست لديك صلاحية لتنفيذ هذا الإجراء.", "Vous n’êtes pas autorisé à effectuer cette action."),
        ["paymentsReceived"] = ("الدفعات المحصلة", "Paiements reçus"), ["paymentTooLarge"] = ("مبلغ الدفعة يتجاوز الرصيد المتبقي.", "Le paiement dépasse le solde restant."),
        ["settings"] = ("الإعدادات", "Paramètres"), ["settled"] = ("مسدد", "Réglé"), ["switchLanguage"] = ("تغيير اللغة", "Changer de langue"), ["unitM3"] = ("م³", "m³"),
        ["darkMode"] = ("الوضع الداكن", "Mode sombre"), ["lightMode"] = ("الوضع الفاتح", "Mode clair"),
        ["allMonths"] = ("كل الأشهر", "Tous les mois"), ["archiveMonth"] = ("شهر الأرشيف", "Mois d’archive"),
        ["monthInvoiceCount"] = ("عدد الفواتير", "Nombre de factures"), ["monthInvoiced"] = ("إجمالي الفواتير", "Total facturé"),
        ["monthPaid"] = ("المدفوع على فواتير الفترة", "Payé sur les factures de la période"), ["monthRemaining"] = ("المتبقي على فواتير الفترة", "Solde des factures de la période")
        , ["users"] = ("المستخدمون", "Utilisateurs"), ["userManagement"] = ("إدارة المستخدمين", "Gestion des utilisateurs")
        , ["userHint"] = ("أنشئ حسابات فردية وحدد مستوى الوصول لكل موظف.", "Créez des comptes individuels et attribuez les droits.")
        , ["createUser"] = ("إنشاء حساب", "Créer un compte"), ["role"] = ("الدور", "Rôle"), ["admin"] = ("مدير النظام", "Administrateur")
        , ["billingRole"] = ("موظف الفوترة", "Agent de facturation"), ["readRole"] = ("قراءة فقط", "Lecture seule")
        , ["temporaryPassword"] = ("كلمة مرور مؤقتة", "Mot de passe provisoire"), ["twoFactor"] = ("التحقق بخطوتين", "Double authentification")
        , ["enabled"] = ("مفعل", "Activé"), ["disabled"] = ("غير مفعل", "Non activé"), ["userAdded"] = ("تم إنشاء الحساب", "Compte créé")
        , ["securityReminder"] = ("فعّل التحقق بخطوتين لحساب المدير من إعدادات الأمان.", "Activez la double authentification du compte administrateur dans les paramètres de sécurité.")
        , ["accessDeniedTitle"] = ("الوصول مرفوض", "Accès refusé"), ["accessDeniedMessage"] = ("ليس لديك إذن للوصول إلى هذا المورد.", "Vous n’avez pas accès à cette ressource.")
        , ["adminTwoFactorRequired"] = ("يلزم تفعيل التحقق بخطوتين لحساب المدير قبل استخدام المنصة.", "L’administrateur doit activer l’authentification à deux facteurs avant d’utiliser la plateforme.")
        , ["configureTwoFactor"] = ("إعداد التحقق بخطوتين", "Configurer l’authentification à deux facteurs")
        , ["lockedOutTitle"] = ("تم قفل الحساب", "Compte verrouillé"), ["lockedOutMessage"] = ("تم قفل هذا الحساب مؤقتاً. حاول مرة أخرى لاحقاً.", "Ce compte a été temporairement verrouillé. Réessayez plus tard.")
        , ["notFoundTitle"] = ("الصفحة غير موجودة", "Page introuvable"), ["notFoundMessage"] = ("الصفحة التي تبحث عنها غير موجودة.", "La page demandée est introuvable.")
        , ["profile"] = ("الملف الشخصي", "Profil")
        , ["externalLogins"] = ("تسجيلات الدخول الخارجية", "Connexions externes"), ["passkeys"] = ("مفاتيح المرور", "Clés d’accès"), ["personalData"] = ("البيانات الشخصية", "Données personnelles")
        , ["twoFactorTitle"] = ("التحقق بخطوتين", "Authentification à deux facteurs"), ["noRecoveryCodes"] = ("لم يتبق أي رمز استرداد.", "Il ne reste aucun code de récupération.")
        , ["generateRecoveryCodes"] = ("أنشئ مجموعة جديدة من رموز الاسترداد", "Générer de nouveaux codes de récupération"), ["recoveryCodesLeft"] = ("متبقي رمز استرداد واحد.", "Il reste un code de récupération.")
        , ["fewRecoveryCodes"] = ("لم يتبق سوى عدد قليل من رموز الاسترداد.", "Il ne reste que quelques codes de récupération.")
        , ["forgetBrowser"] = ("إزالة تذكر هذا المتصفح", "Oublier ce navigateur"), ["disableTwoFactor"] = ("إيقاف التحقق بخطوتين", "Désactiver l’authentification à deux facteurs")
        , ["resetRecoveryCodes"] = ("إعادة إنشاء رموز الاسترداد", "Réinitialiser les codes de récupération"), ["authenticatorApp"] = ("تطبيق المصادقة", "Application d’authentification")
        , ["addAuthenticator"] = ("إضافة تطبيق مصادقة", "Ajouter une application d’authentification"), ["setupAuthenticator"] = ("إعداد تطبيق المصادقة", "Configurer l’application d’authentification")
        , ["resetAuthenticator"] = ("إعادة ضبط تطبيق المصادقة", "Réinitialiser l’application d’authentification"), ["trackingConsentMissing"] = ("لم تتم الموافقة على سياسة الخصوصية وملفات الارتباط.", "La politique de confidentialité et des cookies n’a pas été acceptée.")
        , ["trackingConsentRequired"] = ("يجب قبول السياسة لتفعيل التحقق بخطوتين.", "Vous devez accepter la politique pour activer l’authentification à deux facteurs.")
        , ["browserForgotten"] = ("تم نسيان هذا المتصفح. سيُطلب رمز التحقق عند تسجيل الدخول منه مجدداً.", "Ce navigateur a été oublié. Un code sera demandé à votre prochaine connexion.")
        , ["profileTitle"] = ("الملف الشخصي", "Profil"), ["accountSettings"] = ("إعدادات الحساب", "Paramètres du compte"), ["username"] = ("اسم المستخدم", "Nom d’utilisateur")
        , ["profilePageHint"] = ("حدّث معلومات الاتصال المرتبطة بحسابك.", "Mettez à jour les coordonnées associées à votre compte.")
        , ["profileSaved"] = ("تم حفظ بيانات الملف الشخصي.", "Les informations du profil ont été enregistrées.")
        , ["profileSaveFailed"] = ("تعذر حفظ رقم الهاتف. تحقق من البيانات ثم أعد المحاولة.", "Impossible d’enregistrer le numéro de téléphone. Vérifiez les données et réessayez.")
        , ["emailSettingsTitle"] = ("البريد الإلكتروني", "Adresse e-mail")
        , ["emailSettingsHint"] = ("اعرض عنوانك الحالي أو اطلب تغييره عبر رسالة تأكيد.", "Consultez votre adresse actuelle ou demandez sa modification par e-mail de confirmation.")
        , ["currentEmail"] = ("البريد الحالي", "Adresse actuelle"), ["emailConfirmed"] = ("تم تأكيد البريد", "Adresse confirmée")
        , ["newEmail"] = ("البريد الجديد", "Nouvelle adresse e-mail"), ["newEmailHint"] = ("أدخل عنوان البريد الجديد", "Saisissez la nouvelle adresse e-mail")
        , ["sendVerificationEmail"] = ("إرسال رسالة التحقق", "Envoyer l’e-mail de vérification")
        , ["requestEmailChange"] = ("إرسال رابط تغيير البريد", "Envoyer le lien de modification")
        , ["emailUnchanged"] = ("هذا هو عنوان البريد الحالي بالفعل.", "Cette adresse est déjà votre adresse actuelle.")
        , ["emailChangeLinkSent"] = ("أرسلنا رابط تأكيد التغيير إلى البريد الجديد.", "Le lien de confirmation a été envoyé à la nouvelle adresse.")
        , ["verificationEmailSent"] = ("أرسلنا رسالة التحقق. راجع بريدك الإلكتروني.", "L’e-mail de vérification a été envoyé. Consultez votre boîte de réception.")
        , ["emailDeliveryFailed"] = ("تعذر إرسال الرسالة حالياً. أعد المحاولة لاحقاً أو تواصل مع مدير النظام.", "L’e-mail n’a pas pu être envoyé. Réessayez plus tard ou contactez l’administrateur.")
        , ["personalDataHint"] = ("نزّل نسخة من بيانات حسابك أو راجع إجراء حذف الحساب.", "Téléchargez une copie de vos données ou consultez la procédure de suppression du compte.")
        , ["yourPersonalData"] = ("بيانات حسابك", "Données de votre compte")
        , ["personalDataDescription"] = ("يمكنك تنزيل نسخة من بياناتك الشخصية المحفوظة في النظام.", "Vous pouvez télécharger une copie des données personnelles enregistrées dans le système.")
        , ["downloadPersonalData"] = ("تنزيل بياناتي", "Télécharger mes données"), ["deleteAccount"] = ("حذف الحساب", "Supprimer le compte")
        , ["permanentActionWarningTitle"] = ("إجراء نهائي", "Action irréversible")
        , ["deleteAccountWarning"] = ("حذف الحساب نهائي ولا يمكن التراجع عنه. ستُحذف بيانات الدخول الشخصية الخاصة بالحساب.", "La suppression du compte est définitive. Les données personnelles de connexion seront supprimées.")
        , ["deleteAccountHint"] = ("راجع أثر الحذف وأكد هويتك قبل المتابعة.", "Vérifiez les conséquences et confirmez votre identité avant de continuer.")
        , ["enterPasswordToConfirm"] = ("أدخل كلمة مرورك لتأكيد العملية", "Saisissez votre mot de passe pour confirmer")
        , ["confirmDeleteAccount"] = ("حذف الحساب نهائياً", "Supprimer définitivement le compte")
        , ["incorrectPassword"] = ("كلمة المرور غير صحيحة.", "Le mot de passe est incorrect.")
        , ["accountSettingsHint"] = ("أدر بيانات الدخول ووسائل حماية حسابك.", "Gérez vos identifiants et les protections de votre compte.")
        , ["changePasswordTitle"] = ("تغيير كلمة المرور", "Modifier le mot de passe")
        , ["changePasswordHint"] = ("حدّث كلمة مرورك بانتظام للحفاظ على أمان حسابك.", "Mettez régulièrement votre mot de passe à jour pour protéger votre compte.")
        , ["currentPassword"] = ("كلمة المرور الحالية", "Mot de passe actuel")
        , ["newPassword"] = ("كلمة المرور الجديدة", "Nouveau mot de passe")
        , ["confirmNewPassword"] = ("تأكيد كلمة المرور الجديدة", "Confirmer le nouveau mot de passe")
        , ["updatePassword"] = ("حفظ كلمة المرور الجديدة", "Enregistrer le nouveau mot de passe")
        , ["passwordChanged"] = ("تم تغيير كلمة المرور بنجاح.", "Le mot de passe a été modifié.")
        , ["passwordChangeFailed"] = ("تعذر تغيير كلمة المرور. تحقق من كلمة المرور الحالية ومتطلبات كلمة المرور الجديدة.", "Impossible de modifier le mot de passe. Vérifiez le mot de passe actuel et les exigences du nouveau mot de passe.")
        , ["passwordSecurityTitle"] = ("حافظ على أمان حسابك", "Protégez votre compte")
        , ["passwordSecurityHint"] = ("اختر كلمة مرور يصعب تخمينها، ولا تشاركها مع أي شخص.", "Choisissez un mot de passe difficile à deviner et ne le partagez avec personne.")
        , ["passwordPolicyHint"] = ("12 حرفاً على الأقل، مع حرف كبير وحرف صغير ورقم. لا تشارك كلمة مرورك.", "12 caractères minimum, avec une majuscule, une minuscule et un chiffre. Ne partagez pas votre mot de passe.")
        , ["errorPrefix"] = ("خطأ: ", "Erreur : ")
        , ["setPasswordTitle"] = ("إضافة كلمة مرور", "Ajouter un mot de passe")
        , ["setPasswordHint"] = ("أضف كلمة مرور محلية لتسجيل الدخول إلى المنصة بهذا الحساب.", "Ajoutez un mot de passe local pour vous connecter à la plateforme avec ce compte.")
        , ["savePassword"] = ("حفظ كلمة المرور", "Enregistrer le mot de passe")
        , ["passwordSet"] = ("تمت إضافة كلمة المرور بنجاح.", "Le mot de passe a été ajouté.")
        , ["passwordSetFailed"] = ("تعذرت إضافة كلمة المرور. تحقق من استيفائها للمتطلبات ثم أعد المحاولة.", "Impossible d’ajouter le mot de passe. Vérifiez qu’il respecte les exigences puis réessayez.")
        , ["resetPasswordFailed"] = ("تعذر تغيير كلمة المرور. ربما انتهت صلاحية الرابط أو لم تستوفِ كلمة المرور المتطلبات.", "Impossible de modifier le mot de passe. Le lien a peut-être expiré ou le mot de passe ne respecte pas les exigences.")
        , ["recoveryCodePlaceholder"] = ("أدخل رمز الاسترداد", "Saisissez le code de récupération")
        , ["invalidUserTitle"] = ("تعذر العثور على الحساب", "Compte introuvable")
        , ["invalidUserHint"] = ("تعذر استعادة بيانات هذا الحساب. سجّل الدخول مجدداً أو تواصل مع مسؤول المنصة.", "Impossible de retrouver les informations de ce compte. Reconnectez-vous ou contactez l’administrateur de la plateforme.")
        , ["externalLoginTitle"] = ("إكمال تسجيل الدخول", "Terminer la connexion")
        , ["externalLoginHint"] = ("تم التحقق من حساب {0}. أدخل بريدك الإلكتروني لإكمال تسجيل الدخول إلى المنصة.", "Le compte {0} a été vérifié. Saisissez votre adresse e-mail pour terminer la connexion à la plateforme.")
        , ["externalLoginEmailHint"] = ("البريد الإلكتروني المرتبط بحسابك", "Adresse e-mail associée à votre compte")
        , ["externalLoginContinue"] = ("متابعة", "Continuer")
        , ["externalLoginFailed"] = ("تعذر إكمال تسجيل الدخول الخارجي. أعد المحاولة أو تواصل مع مسؤول المنصة.", "Impossible de terminer la connexion externe. Réessayez ou contactez l’administrateur de la plateforme.")
        , ["externalLoginAccountFailed"] = ("تعذر إنشاء الحساب المرتبط. تواصل مع مسؤول المنصة.", "Impossible de créer le compte associé. Contactez l’administrateur de la plateforme.")
        , ["passkeysTitle"] = ("مفاتيح المرور", "Clés d’accès")
        , ["passkeysHint"] = ("سجّل الدخول بأمان باستخدام بصمة الجهاز أو قفل الشاشة.", "Connectez-vous en toute sécurité avec la biométrie ou le verrouillage de votre appareil.")
        , ["passkeyRenameTitle"] = ("سمّ مفتاح المرور", "Nommer la clé d’accès")
        , ["passkeyRenameHint"] = ("اختر اسماً يسهل عليك تمييز هذا الجهاز.", "Choisissez un nom qui vous aidera à reconnaître cet appareil.")
        , ["passkeyName"] = ("اسم مفتاح المرور", "Nom de la clé d’accès")
        , ["continue"] = ("متابعة", "Continuer")
        , ["passkeyNameTooLong"] = ("يجب ألا يتجاوز اسم مفتاح المرور 200 حرف.", "Le nom de la clé d’accès ne doit pas dépasser 200 caractères.")
        , ["passkeyNotFound"] = ("لم يتم العثور على مفتاح المرور.", "La clé d’accès est introuvable.")
        , ["noPasskeys"] = ("لا توجد مفاتيح مرور مسجلة", "Aucune clé d’accès enregistrée")
        , ["noPasskeysHint"] = ("أضف مفتاح مرور لاستخدام جهازك في تسجيل الدخول بأمان.", "Ajoutez une clé pour vous connecter de façon sécurisée avec votre appareil.")
        , ["unnamedPasskey"] = ("مفتاح مرور بدون اسم", "Clé d’accès sans nom")
        , ["renamePasskey"] = ("إعادة التسمية", "Renommer")
        , ["removePasskey"] = ("إزالة", "Supprimer")
        , ["addPasskey"] = ("إضافة مفتاح مرور", "Ajouter une clé d’accès")
        , ["passkeyLimitReached"] = ("بلغت الحد الأقصى لمفاتيح المرور. أزل مفتاحاً قبل إضافة آخر.", "Vous avez atteint le nombre maximal de clés. Supprimez-en une avant d’en ajouter une autre.")
        , ["passkeyAddFailed"] = ("تعذرت إضافة مفتاح المرور. تحقق من إعدادات الجهاز وحاول مجدداً.", "Impossible d’ajouter la clé. Vérifiez les paramètres de votre appareil et réessayez.")
        , ["passkeyDeleteFailed"] = ("تعذرت إزالة مفتاح المرور.", "Impossible de supprimer la clé d’accès.")
        , ["passkeyDeleted"] = ("تمت إزالة مفتاح المرور.", "La clé d’accès a été supprimée.")
        , ["passkeyRenameFailed"] = ("تعذرت إعادة تسمية مفتاح المرور.", "Impossible de renommer la clé d’accès.")
        , ["passkeyRenamed"] = ("تم تحديث اسم مفتاح المرور.", "Le nom de la clé d’accès a été mis à jour.")
        , ["invalidPasskeyId"] = ("معرّف مفتاح المرور غير صالح.", "L’identifiant de la clé d’accès est invalide.")
        , ["passkeyUnavailable"] = ("لم يستجب الجهاز لمفتاح المرور. حاول مجدداً.", "L’appareil n’a pas fourni de clé d’accès. Réessayez.")
        , ["passkeyUnknownAction"] = ("الطلب غير معروف. أعد المحاولة.", "Action inconnue. Veuillez réessayer.")
        , ["twoFactorSetupHint"] = ("أضف طبقة تحقق إضافية لحماية تسجيل الدخول إلى حسابك.", "Ajoutez une vérification supplémentaire pour protéger la connexion à votre compte.")
        , ["twoFactorEnabled"] = ("التحقق بخطوتين مفعّل", "Authentification à deux facteurs activée")
        , ["twoFactorDisabled"] = ("التحقق بخطوتين غير مفعّل", "Authentification à deux facteurs désactivée")
        , ["twoFactorEnabledHint"] = ("سيُطلب رمز من تطبيق المصادقة عند تسجيل الدخول.", "Un code de l’application d’authentification sera demandé à la connexion.")
        , ["twoFactorDisabledHint"] = ("فعّل تطبيق مصادقة حتى تضيف خطوة تحقق ثانية إلى حسابك.", "Configurez une application d’authentification pour ajouter une seconde vérification.")
        , ["authenticatorAppHint"] = ("استخدم تطبيق مصادقة يولّد رموزاً مؤقتة، واحتفظ برموز الاسترداد في مكان آمن.", "Utilisez une application qui génère des codes temporaires et conservez les codes de récupération en lieu sûr.")
        , ["twoFactorCodesHint"] = ("لا تشارك مفتاح المصادقة أو رموز الاسترداد مع أي شخص.", "Ne partagez jamais la clé d’authentification ni les codes de récupération.")
        , ["authenticatorSetupTitle"] = ("إعداد تطبيق المصادقة", "Configurer l’application d’authentification")
        , ["authenticatorStepOne"] = ("افتح تطبيق المصادقة واختر إضافة حساب جديد.", "Ouvrez votre application d’authentification et ajoutez un nouveau compte.")
        , ["authenticatorStepTwo"] = ("أدخل المفتاح التالي يدوياً، واختر نوع الرمز المستند إلى الوقت (TOTP).", "Saisissez manuellement la clé ci-dessous et choisissez un code temporel (TOTP).")
        , ["authenticatorSecret"] = ("مفتاح الإعداد", "Clé de configuration")
        , ["authenticatorAccount"] = ("الحساب", "Compte")
        , ["authenticatorIssuer"] = ("الجهة", "Émetteur")
        , ["authenticatorStepThree"] = ("أدخل الرمز المكوّن من 6 أرقام الذي يظهر في التطبيق لتأكيد الإعداد.", "Saisissez le code à 6 chiffres affiché dans l’application pour confirmer la configuration.")
        , ["verificationCode"] = ("رمز التحقق", "Code de vérification")
        , ["verifyAndEnable"] = ("تحقق وفعّل", "Vérifier et activer")
        , ["invalidAuthenticatorCode"] = ("رمز التحقق غير صحيح أو انتهت صلاحيته. حاول مجدداً.", "Code incorrect ou expiré. Veuillez réessayer.")
        , ["authenticatorEnabledMessage"] = ("تم تفعيل التحقق بخطوتين.", "L’authentification à deux facteurs est activée.")
        , ["authenticatorAlreadyEnabled"] = ("المصادقة مفعلة بالفعل. لا نعرض مفتاحها الحالي مجدداً.", "L’authentification est déjà activée. La clé actuelle ne sera pas réaffichée.")
        , ["authenticatorEnableFailed"] = ("تعذر تفعيل التحقق بخطوتين. أعد المحاولة.", "Impossible d’activer l’authentification à deux facteurs. Réessayez.")
        , ["accountTemporarilyLocked"] = ("تم إيقاف المحاولات مؤقتاً بسبب تكرار الرمز الخاطئ. سجّل الدخول مجدداً بعد انتهاء القفل.", "Les tentatives sont temporairement bloquées après plusieurs codes erronés. Reconnectez-vous après le délai de verrouillage.")
        , ["authenticatorDisableWarning"] = ("سيؤدي هذا الإجراء إلى إيقاف التحقق الإضافي لحسابك.", "Cette action désactive la vérification supplémentaire de votre compte.")
        , ["authenticatorResetWarning"] = ("سيُلغى مفتاح التطبيق الحالي ويتوقف التحقق بخطوتين حتى تعيد ربط التطبيق.", "La clé actuelle sera révoquée et l’authentification à deux facteurs restera désactivée jusqu’à la nouvelle configuration.")
        , ["confirmDisableTwoFactor"] = ("تأكيد إيقاف التحقق بخطوتين", "Confirmer la désactivation de l’authentification à deux facteurs")
        , ["confirmResetAuthenticator"] = ("إعادة ضبط مفتاح المصادقة", "Réinitialiser la clé d’authentification")
        , ["confirmGenerateRecoveryCodes"] = ("إنشاء رموز جديدة", "Générer de nouveaux codes")
        , ["operationFailed"] = ("تعذر تنفيذ العملية. أعد المحاولة.", "L’opération a échoué. Veuillez réessayer.")
        , ["recoveryCodesWarning"] = ("تُعرض رموز الاسترداد مرة واحدة فقط. احفظها الآن في مكان آمن.", "Les codes de récupération ne s’affichent qu’une seule fois. Enregistrez-les dans un endroit sûr.")
        , ["recoveryCodesTitle"] = ("رموز استرداد الحساب", "Codes de récupération du compte")
        , ["recoveryCodesResetWarning"] = ("إنشاء مجموعة جديدة يُبطل جميع رموز الاسترداد السابقة.", "La création d’un nouveau jeu invalide tous les anciens codes de récupération.")
        , ["returnToSecuritySettings"] = ("العودة إلى إعدادات الأمان", "Retour aux paramètres de sécurité")
        , ["phoneNumber"] = ("رقم الهاتف", "Numéro de téléphone"), ["enterPhoneNumber"] = ("أدخل رقم هاتفك", "Saisissez votre numéro de téléphone"), ["saveChanges"] = ("حفظ التغييرات", "Enregistrer les modifications")
        , ["loginErrorPageTitle"] = ("حدث خطأ", "Une erreur s’est produite"), ["loginErrorPageMessage"] = ("حدث خطأ أثناء معالجة طلبك.", "Une erreur est survenue lors du traitement de votre demande.")
        , ["requestId"] = ("معرّف الطلب", "Identifiant de requête"), ["errorDetailsDevelopment"] = ("وضع التطوير", "Mode développement"), ["errorDetailsHint"] = ("يمكن عرض تفاصيل إضافية للأخطاء في بيئة التطوير فقط.", "Les détails des erreurs sont disponibles uniquement en environnement de développement.")
        , ["developmentWarning"] = ("لا تستخدم وضع التطوير في التطبيقات المنشورة لأنه قد يعرض معلومات حساسة.", "N’utilisez pas le mode développement en production : il peut exposer des informations sensibles.")
        , ["notAuthenticatedTitle"] = ("تم التحقق من تسجيل الدخول", "Authentification confirmée"), ["loggedInAs"] = ("تم تسجيل الدخول باسم", "Connecté en tant que")
        , ["twoFactorLoginHint"] = ("حسابك محمي بتطبيق المصادقة. أدخل الرمز الظاهر في التطبيق.", "Votre compte est protégé par une application d’authentification. Saisissez le code affiché.")
        , ["authenticatorCode"] = ("رمز المصادقة", "Code d’authentification"), ["rememberThisDevice"] = ("تذكر هذا الجهاز", "Mémoriser cet appareil")
        , ["loginButton"] = ("تسجيل الدخول", "Se connecter"), ["noAuthenticatorAccess"] = ("لا يمكنك الوصول إلى تطبيق المصادقة؟", "Vous n’avez plus accès à l’application d’authentification ?")
        , ["useRecoveryCode"] = ("استخدم رمز استرداد", "Utiliser un code de récupération"), ["recoveryCodeTitle"] = ("التحقق برمز الاسترداد", "Vérification par code de récupération")
        , ["recoveryLoginHint"] = ("لن يتم تذكر هذا الجهاز عند استخدام رمز الاسترداد.", "Cet appareil ne sera pas mémorisé lors de l’utilisation d’un code de récupération.")
        , ["recoveryCode"] = ("رمز الاسترداد", "Code de récupération")
        , ["invalidRecoveryCode"] = ("رمز الاسترداد غير صالح.", "Le code de récupération est invalide.")
    };

    public static IReadOnlyCollection<string> Keys => Text.Keys;

    public string this[string key]
    {
        get
        {
            if (!Text.TryGetValue(key, out var value)) return key;
            return CultureInfo.CurrentUICulture.Name.StartsWith("fr", StringComparison.OrdinalIgnoreCase) ? value.Fr : value.Ar;
        }
    }
}
